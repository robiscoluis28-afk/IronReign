using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Services;

namespace IronReign.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private const int WeeklyGoalSessions = 4;

    private readonly UserSessionService _userSessionService;
    private readonly AppDatabase _database;

    [ObservableProperty]
    public partial string WelcomeText { get; set; } = "Bienvenido";

    [ObservableProperty]
    public partial string ActiveUserText { get; set; } = "Sin perfil activo";

    [ObservableProperty]
    public partial string WeeklyTimeText { get; set; } = "0 min";

    [ObservableProperty]
    public partial string MonthlyWorkoutsText { get; set; } = "0";

    [ObservableProperty]
    public partial double WeeklyGoalProgress { get; set; }

    [ObservableProperty]
    public partial string WeeklyGoalText { get; set; } = $"0 de {WeeklyGoalSessions} entrenos esta semana";

    [ObservableProperty]
    public partial string SummaryCaptionText { get; set; } = "Sin entrenamientos todavía";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

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
                ActiveUserText = "Sin perfil activo";
                WelcomeText = "Bienvenido";
                ResetStats();
                return;
            }

            ActiveUserText = $"{user.FirstName} {user.LastName}".Trim();
            WelcomeText = $"Hola, {user.FirstName}";

            var sessions = await _database.GetWorkoutSessionsByUserAsync(user.Id);

            if (sessions.Count == 0)
            {
                ResetStats();
                return;
            }

            var today = DateTime.UtcNow.Date;
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            var mondayThisWeek = today.AddDays(-daysSinceMonday);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var weeklySessions = sessions.Where(s => s.SessionDateUtc.Date >= mondayThisWeek).ToList();
            var monthlySessions = sessions.Where(s => s.SessionDateUtc.Date >= monthStart).ToList();

            WeeklyTimeText = $"{weeklySessions.Sum(s => s.DurationMinutes)} min";
            MonthlyWorkoutsText = monthlySessions.Count.ToString();

            WeeklyGoalProgress = Math.Clamp((double)weeklySessions.Count / WeeklyGoalSessions, 0, 1);
            WeeklyGoalText = $"{Math.Min(weeklySessions.Count, WeeklyGoalSessions)} de {WeeklyGoalSessions} entrenos esta semana";

            int totalSets = 0;
            int sessionCountWithSets = 0;

            foreach (var session in sessions)
            {
                var blocks = await _database.GetWorkoutExerciseBlocksAsync(session.Id);
                int sessionSets = 0;

                foreach (var block in blocks)
                {
                    var entries = await _database.GetWorkoutBlockEntriesAsync(block.Id);
                    sessionSets += entries.Count;
                }

                if (sessionSets > 0)
                {
                    totalSets += sessionSets;
                    sessionCountWithSets++;
                }
            }

            var averageSets = sessionCountWithSets > 0
                ? Math.Round((double)totalSets / sessionCountWithSets, 1)
                : 0;

            var lastSession = sessions.First();
            SummaryCaptionText = $"Último: {lastSession.SessionDateUtc:dd/MM} · Promedio {averageSets} series/sesión";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetStats()
    {
        WeeklyTimeText = "0 min";
        MonthlyWorkoutsText = "0";
        WeeklyGoalProgress = 0;
        WeeklyGoalText = $"0 de {WeeklyGoalSessions} entrenos esta semana";
        SummaryCaptionText = "Sin entrenamientos todavía";
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
