using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Models;
using IronReign.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace IronReign.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly UserSessionService _userSessionService;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<WorkoutSessionSummaryViewModel> Sessions { get; } = new();

    public HistoryViewModel(AppDatabase database, UserSessionService userSessionService)
    {
        _database = database;
        _userSessionService = userSessionService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Sessions.Clear();

            var activeUser = await _userSessionService.LoadActiveUserAsync();

            if (activeUser is null)
            {
                ErrorMessage = "No hay un usuario activo.";
                return;
            }

            var sessions = await _database.GetWorkoutSessionsByUserAsync(activeUser.Id);

            foreach (var session in sessions)
            {
                var summary = new WorkoutSessionSummaryViewModel
                {
                    Id = session.Id,
                    Name = string.IsNullOrWhiteSpace(session.Name) ? "Entreno" : session.Name,
                    DateText = session.SessionDateUtc.ToLocalTime().ToString("dddd d MMMM · HH:mm", CultureInfo.GetCultureInfo("es-ES")),
                    Notes = session.Notes
                };

                var blocks = await _database.GetWorkoutExerciseBlocksAsync(session.Id);

                var totalSets = 0;
                var totalVolume = 0.0;

                foreach (var block in blocks.OrderBy(x => x.DisplayOrder))
                {
                    var entries = await _database.GetWorkoutBlockEntriesAsync(block.Id);

                    var setsText = string.Join(" · ", entries
                        .OrderBy(e => e.EntryOrder)
                        .Select(FormatEntry));

                    totalSets += entries.Count;
                    totalVolume += entries.Sum(e => e.Weight * e.Reps);

                    summary.Exercises.Add(new WorkoutBlockSummaryViewModel
                    {
                        ExerciseName = block.ExerciseName,
                        BlockType = block.BlockType,
                        SetsText = string.IsNullOrWhiteSpace(setsText) ? "Sin series registradas" : setsText,
                        Notes = block.Notes
                    });
                }

                summary.TotalSets = totalSets;
                summary.TotalVolumeText = $"{totalVolume:0.##} kg totales";

                Sessions.Add(summary);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar el historial: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSessionAsync(WorkoutSessionSummaryViewModel session)
    {
        if (session is null)
            return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Borrar entreno",
            $"¿Seguro que quieres borrar \"{session.Name}\" del {session.DateText}?",
            "Borrar",
            "Cancelar");

        if (!confirmed)
            return;

        var activeUser = await _userSessionService.LoadActiveUserAsync();

        if (activeUser is null)
            return;

        var sessions = await _database.GetWorkoutSessionsByUserAsync(activeUser.Id);
        var sessionEntity = sessions.FirstOrDefault(x => x.Id == session.Id);

        if (sessionEntity is null)
            return;

        await _database.DeleteWorkoutSessionAsync(sessionEntity);
        Sessions.Remove(session);
    }

    private static string FormatEntry(WorkoutBlockEntry entry)
    {
        var text = $"{entry.Weight:0.##}kg x {entry.Reps}";

        if (entry.Rir.HasValue)
            text += $" @RIR{entry.Rir}";

        if (!string.IsNullOrWhiteSpace(entry.EntryType) && entry.EntryType != "Main")
            text = $"{entry.EntryType}: {text}";

        return text;
    }
}

public partial class WorkoutBlockSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BlockType { get; set; } = "Normal";

    [ObservableProperty]
    public partial string SetsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;
}

public partial class WorkoutSessionSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotalSets { get; set; }

    [ObservableProperty]
    public partial string TotalVolumeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public ObservableCollection<WorkoutBlockSummaryViewModel> Exercises { get; } = new();

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }
}