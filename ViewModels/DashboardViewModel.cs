using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Services;
using System.Collections.ObjectModel;

namespace IronReign.ViewModels;

public class StreakDayItem
{
    public bool Trained { get; init; }
    public bool IsToday { get; init; }
}

public partial class DashboardViewModel : ObservableObject
{
    private const int StreakDaysWindow = 28;
    private const int TrendWeeks = 8;

    private readonly UserSessionService _userSessionService;
    private readonly AppDatabase _database;

    [ObservableProperty]
    public partial string WelcomeText { get; set; } = "Bienvenido";

    [ObservableProperty]
    public partial bool HasTodayRoutine { get; set; }

    [ObservableProperty]
    public partial string TodayRoutineName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TodayExerciseCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CurrentStreakDays { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public ObservableCollection<StreakDayItem> StreakDays { get; } = new();

    public ObservableCollection<double> WeeklyTrend { get; } = new();

    public DashboardViewModel(UserSessionService userSessionService, AppDatabase database)
    {
        _userSessionService = userSessionService;
        _database = database;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var user = await _userSessionService.LoadActiveUserAsync();

            if (user is null)
            {
                WelcomeText = "Bienvenido";
                HasTodayRoutine = false;
                CurrentStreakDays = 0;
                RefreshStreak(new HashSet<DateTime>());
                RefreshTrend(new double[TrendWeeks]);
                return;
            }

            WelcomeText = $"Hola, {user.FirstName}";

            var todayRoutine = await _database.GetRoutineForDayAsync(user.Id, DateTime.Now.DayOfWeek);

            if (todayRoutine is null)
            {
                HasTodayRoutine = false;
                TodayRoutineName = string.Empty;
                TodayExerciseCountText = string.Empty;
            }
            else
            {
                var exercises = await _database.GetRoutineExercisesAsync(todayRoutine.Id);
                HasTodayRoutine = true;
                TodayRoutineName = todayRoutine.Name;
                TodayExerciseCountText = $"{exercises.Count} ejercicios";
            }

            var sessions = await _database.GetWorkoutSessionsByUserAsync(user.Id);
            var trainedDates = sessions.Select(s => s.SessionDateUtc.Date).ToHashSet();

            RefreshStreak(trainedDates);

            var today = DateTime.UtcNow.Date;
            var weeklyMinutes = new double[TrendWeeks];

            foreach (var session in sessions)
            {
                var daysAgo = (today - session.SessionDateUtc.Date).Days;
                var weekIndex = TrendWeeks - 1 - (daysAgo / 7);

                if (weekIndex >= 0 && weekIndex < TrendWeeks)
                    weeklyMinutes[weekIndex] += session.DurationMinutes;
            }

            RefreshTrend(weeklyMinutes);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshStreak(HashSet<DateTime> trainedDates)
    {
        var today = DateTime.UtcNow.Date;

        StreakDays.Clear();

        for (var i = StreakDaysWindow - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            StreakDays.Add(new StreakDayItem
            {
                Trained = trainedDates.Contains(date),
                IsToday = date == today
            });
        }

        var streak = 0;
        var cursor = today;

        if (!trainedDates.Contains(cursor))
            cursor = cursor.AddDays(-1);

        while (trainedDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        CurrentStreakDays = streak;
    }

    private void RefreshTrend(IReadOnlyList<double> weeklyMinutes)
    {
        WeeklyTrend.Clear();

        foreach (var minutes in weeklyMinutes)
            WeeklyTrend.Add(minutes);
    }

    [RelayCommand]
    private async Task GoToWorkoutAsync()
    {
        await Shell.Current.GoToAsync("//workoutrunner");
    }

    [RelayCommand]
    private async Task GoToRoutinesAsync()
    {
        await Shell.Current.GoToAsync("//routines");
    }
}
