using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Models;
using IronReign.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace IronReign.ViewModels;

public partial class WorkoutRunnerViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly UserSessionService _userSessionService;
    private readonly WorkoutSessionState _sessionState;
    private readonly CloudBackupService _cloudBackupService;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;


    public ObservableCollection<WorkoutRoutineListItemViewModel> Routines { get; } = new();

    public WorkoutRunnerViewModel(
        AppDatabase database,
        UserSessionService userSessionService,
        WorkoutSessionState sessionState,
        CloudBackupService cloudBackupService)
    {
        _database = database;
        _userSessionService = userSessionService;
        _sessionState = sessionState;
        _cloudBackupService = cloudBackupService;
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Routines.Clear();

            var activeUser = await _userSessionService.LoadActiveUserAsync();

            if (activeUser is null)
            {
                ErrorMessage = "No hay un usuario activo.";
                return;
            }

            var routines = await _database.GetRoutineTemplatesByUserAsync(activeUser.Id);

            foreach (var routine in routines.OrderBy(x => x.DisplayOrder))
            {
                var routineVm = new WorkoutRoutineListItemViewModel
                {
                    Id = routine.Id,
                    Name = routine.Name,
                    IsExpanded = false,
                    IsWorkoutActive = false,
                    ElapsedTimeDisplay = "00:00:00"
                };

                routineVm.StartWorkoutCommand = new RelayCommand(() => StartWorkout(routineVm));
                routineVm.FinishWorkoutCommand = new RelayCommand(async () => await FinishWorkoutAsync(routineVm));
                routineVm.ToggleExpandCommand = new RelayCommand(() => routineVm.IsExpanded = !routineVm.IsExpanded);

                var exercises = await _database.GetRoutineExercisesAsync(routine.Id);

                foreach (var exercise in exercises.OrderBy(x => x.DisplayOrder))
                {
                    var techniques = await _database.GetSetTechniquesAsync(exercise.Id);

                    var runItem = new WorkoutExerciseRunItemViewModel
                    {
                        RoutineExerciseId = exercise.Id,
                        ExerciseName = exercise.ExerciseName,
                        BlockType = exercise.BlockType,
                        TargetRepsRaw = exercise.TargetReps ?? string.Empty,
                        TotalSets = exercise.PlannedSets,
                        CompletedSets = 0,
                        SetTechniques = techniques,
                        TargetReps = string.IsNullOrWhiteSpace(exercise.TargetReps)
                            ? "Sin objetivo de reps"
                            : $"Objetivo: {exercise.TargetReps}",
                        LastPerformance = exercise.SuggestedWeight > 0
                            ? $"Peso sugerido: {exercise.SuggestedWeight:0.##} kg"
                            : "Sin peso sugerido",
                        WeightText = exercise.SuggestedWeight > 0
                            ? exercise.SuggestedWeight.ToString("0.##", CultureInfo.InvariantCulture)
                            : string.Empty,
                        RepsText = string.Empty,
                        PlannedRestSeconds = exercise.RestSeconds > 0 ? exercise.RestSeconds : 90
                    };

                    runItem.RefreshCurrentStep();
                    routineVm.Exercises.Add(runItem);
                }

                Routines.Add(routineVm);
            }

            RestoreActiveWorkout();

            if (Routines.Count == 0)
                ErrorMessage = "Todavía no hay rutinas disponibles.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar entrenos: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    public void RestoreActiveWorkout()
    {
        foreach (var routine in Routines)
        {
            var isActive = _sessionState.HasActiveWorkout && routine.Id == _sessionState.ActiveRoutineId;

            routine.IsWorkoutActive = isActive;
            routine.SetElapsed(isActive ? _sessionState.GetElapsedSeconds() : 0);

            foreach (var exercise in routine.Exercises)
                exercise.IsWorkoutActive = isActive;
        }
    }

    [RelayCommand]
    private void StartWorkout(WorkoutRoutineListItemViewModel? routine)
    {
        if (routine is null)
            return;

        _sessionState.Start(routine.Id);

        foreach (var item in Routines)
        {
            item.IsWorkoutActive = item.Id == routine.Id;
            item.SetElapsed(item.Id == routine.Id ? _sessionState.GetElapsedSeconds() : 0);

            foreach (var exercise in item.Exercises)
            {
                exercise.IsWorkoutActive = item.IsWorkoutActive;

                if (item.Id == routine.Id)
                    exercise.ResetForNewWorkout();
            }
        }
    }

    [RelayCommand]
    private async Task FinishWorkoutAsync(WorkoutRoutineListItemViewModel? routine)
    {
        if (routine is null)
            return;

        var elapsedSeconds = _sessionState.GetElapsedSeconds();
        _sessionState.Stop();

        try
        {
            var activeUser = await _userSessionService.LoadActiveUserAsync();
            var exercisesWithWork = routine.Exercises.Where(e => e.CompletedEntries.Count > 0).ToList();

            if (activeUser is not null && exercisesWithWork.Count > 0)
            {
                var session = new WorkoutSession
                {
                    UserProfileId = activeUser.Id,
                    Name = routine.Name,
                    SessionDateUtc = DateTime.UtcNow,
                    DurationMinutes = Math.Max(1, elapsedSeconds / 60),
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _database.SaveWorkoutSessionAsync(session);

                var displayOrder = 1;

                foreach (var exercise in exercisesWithWork)
                {
                    var block = new WorkoutExerciseBlock
                    {
                        WorkoutSessionId = session.Id,
                        RoutineExerciseId = exercise.RoutineExerciseId,
                        DisplayOrder = displayOrder++,
                        ExerciseName = exercise.ExerciseName,
                        BlockType = exercise.BlockType,
                        PlannedSets = exercise.TotalSets,
                        TargetReps = exercise.TargetRepsRaw,
                        IsCompleted = exercise.CompletedSets >= exercise.TotalSets
                    };

                    await _database.SaveWorkoutExerciseBlockAsync(block);

                    var entryOrder = 1;

                    foreach (var entry in exercise.CompletedEntries)
                    {
                        await _database.SaveWorkoutBlockEntryAsync(new WorkoutBlockEntry
                        {
                            WorkoutExerciseBlockId = block.Id,
                            WorkoutSessionId = session.Id,
                            UserProfileId = activeUser.Id,
                            EntryOrder = entryOrder++,
                            EntryType = entry.EntryType,
                            Weight = entry.Weight,
                            Reps = entry.Reps,
                            Rir = entry.Rir,
                            RestSeconds = entry.RestSeconds
                        });
                    }
                }

                if (_cloudBackupService.IsConfigured)
                    _ = _cloudBackupService.BackupAsync(activeUser);

                var totalCompletedSets = exercisesWithWork.Sum(e => e.CompletedEntries.Count);
                var durationMinutes = Math.Max(1, elapsedSeconds / 60);

                await Shell.Current.DisplayAlert(
                    "Entreno guardado",
                    $"{routine.Name}\n\nDuración: {durationMinutes} min\nSeries completadas: {totalCompletedSets}",
                    "OK");
            }
            else if (activeUser is not null)
            {
                await Shell.Current.DisplayAlert(
                    "Entreno no guardado",
                    "No se ha guardado nada porque no completaste ninguna serie.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo guardar el entreno: {ex.Message}";
        }
        finally
        {
            foreach (var item in Routines)
            {
                item.IsWorkoutActive = false;
                item.SetElapsed(0);

                foreach (var exercise in item.Exercises)
                {
                    exercise.IsWorkoutActive = false;
                    exercise.ResetForNewWorkout();
                }
            }
        }
    }

    public void Tick()
    {
        if (!_sessionState.HasActiveWorkout)
            return;

        var activeRoutine = Routines.FirstOrDefault(x => x.Id == _sessionState.ActiveRoutineId);
        if (activeRoutine is null)
            return;

        activeRoutine.IsWorkoutActive = true;
        activeRoutine.SetElapsed(_sessionState.GetElapsedSeconds());

        foreach (var exercise in activeRoutine.Exercises)
            exercise.IsWorkoutActive = true;
    }
}