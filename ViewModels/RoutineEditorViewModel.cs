using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Models;
using IronReign.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace IronReign.ViewModels;

public partial class DayToggleItem : ObservableObject
{
    public required DayOfWeek Day { get; init; }
    public required string Label { get; init; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public partial class RoutineEditorViewModel : ObservableObject, IQueryAttributable
{
    private readonly AppDatabase _database;
    private readonly UserSessionService _userSessionService;

    private HashSet<int> _loadedExerciseIds = new();

    [ObservableProperty]
    public partial int RoutineId { get; set; }

    [ObservableProperty]
    public partial string RoutineName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RoutineNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewExerciseBlockType { get; set; } = "Normal";

    [ObservableProperty]
    public partial string NewExercisePlannedSets { get; set; } = "3";

    [ObservableProperty]
    public partial string NewExerciseTargetReps { get; set; } = "8-12";

    [ObservableProperty]
    public partial string NewExerciseSuggestedWeight { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewExerciseRestSeconds { get; set; } = "90";

    [ObservableProperty]
    public partial string NewExerciseNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool NewExerciseIsSuperset { get; set; }

    [ObservableProperty]
    public partial RoutineExerciseEditorItemViewModel? NewExerciseSupersetPartner { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingExercise))]
    public partial RoutineExerciseEditorItemViewModel? EditingExercise { get; set; }

    public bool IsEditingExercise => EditingExercise is not null;

    public ObservableCollection<string> SetTechniqueOptions { get; } = new()
    {
        "Normal",
        "Dropset",
        "Dropset doble",
        "Cluster",
        "Myo-reps"
    };

    public ObservableCollection<DayToggleItem> ScheduledDayOptions { get; } = new()
    {
        new DayToggleItem { Day = DayOfWeek.Monday, Label = "L" },
        new DayToggleItem { Day = DayOfWeek.Tuesday, Label = "M" },
        new DayToggleItem { Day = DayOfWeek.Wednesday, Label = "X" },
        new DayToggleItem { Day = DayOfWeek.Thursday, Label = "J" },
        new DayToggleItem { Day = DayOfWeek.Friday, Label = "V" },
        new DayToggleItem { Day = DayOfWeek.Saturday, Label = "S" },
        new DayToggleItem { Day = DayOfWeek.Sunday, Label = "D" }
    };

    public ObservableCollection<SetTechniqueItemViewModel> NewExerciseSetTechniques { get; } = new();

    public ObservableCollection<RoutineExerciseEditorItemViewModel> AvailableSupersetPartners { get; } = new();

    public ObservableCollection<RoutineExerciseEditorItemViewModel> Exercises { get; } = new();

    public RoutineEditorViewModel(
        AppDatabase database,
        UserSessionService userSessionService)
    {
        _database = database;
        _userSessionService = userSessionService;
    }

    partial void OnNewExercisePlannedSetsChanged(string value) => SyncNewExerciseSetTechniques(value);

    private void SyncNewExerciseSetTechniques(string plannedSetsText)
    {
        if (!int.TryParse(plannedSetsText, out var plannedSets) || plannedSets <= 0)
            return;

        while (NewExerciseSetTechniques.Count < plannedSets)
        {
            NewExerciseSetTechniques.Add(new SetTechniqueItemViewModel
            {
                SetIndex = NewExerciseSetTechniques.Count + 1
            });
        }

        while (NewExerciseSetTechniques.Count > plannedSets)
        {
            NewExerciseSetTechniques.RemoveAt(NewExerciseSetTechniques.Count - 1);
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var routineId = 0;

        if (query.TryGetValue("RoutineId", out var value) && value is not null)
            int.TryParse(value.ToString(), out routineId);

        _ = LoadAsync(routineId);
    }

    public async Task LoadAsync(int routineId)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            RoutineId = routineId;
            Exercises.Clear();
            _loadedExerciseIds.Clear();

            if (routineId == 0)
            {
                RoutineName = string.Empty;
                RoutineNotes = string.Empty;
                SetSelectedScheduledDays(Enumerable.Empty<int>());
                ResetExerciseInputs();
                RefreshAvailableSupersetPartners();
                return;
            }

            var routine = await _database.GetRoutineTemplateByIdAsync(routineId);

            if (routine is null)
            {
                ErrorMessage = "No se encontró la rutina.";
                return;
            }

            RoutineName = routine.Name;
            RoutineNotes = routine.Notes ?? string.Empty;

            var scheduledDays = (routine.ScheduledDays ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse);

            SetSelectedScheduledDays(scheduledDays);

            var exercises = await _database.GetRoutineExercisesAsync(routine.Id);
            var itemsById = new Dictionary<int, RoutineExerciseEditorItemViewModel>();

            foreach (var exercise in exercises.OrderBy(x => x.DisplayOrder))
            {
                var item = new RoutineExerciseEditorItemViewModel
                {
                    Id = exercise.Id,
                    DisplayOrder = exercise.DisplayOrder,
                    ExerciseName = exercise.ExerciseName,
                    BlockType = exercise.BlockType,
                    PlannedSets = exercise.PlannedSets,
                    TargetReps = exercise.TargetReps,
                    SuggestedWeight = exercise.SuggestedWeight,
                    RestSeconds = exercise.RestSeconds,
                    Notes = exercise.Notes
                };

                item.SyncSetTechniqueCount(exercise.PlannedSets);

                var savedTechniques = await _database.GetSetTechniquesAsync(exercise.Id);
                foreach (var technique in savedTechniques)
                {
                    var target = item.SetTechniques.FirstOrDefault(x => x.SetIndex == technique.SetIndex);
                    if (target is null)
                        continue;

                    target.TechniqueType = technique.TechniqueType;
                    target.DropWeight = technique.DropWeight.ToString(CultureInfo.InvariantCulture);
                    target.DropReps = technique.DropReps;
                    target.DropWeight2 = technique.DropWeight2.ToString(CultureInfo.InvariantCulture);
                    target.DropReps2 = technique.DropReps2;
                    target.ClusterWeight = technique.ClusterWeight.ToString(CultureInfo.InvariantCulture);
                    target.ClusterRepsPerMiniSet = technique.ClusterRepsPerMiniSet;
                    target.ClusterMiniSetCount = technique.ClusterMiniSetCount.ToString(CultureInfo.InvariantCulture);
                    target.ClusterRestSeconds = technique.ClusterRestSeconds.ToString(CultureInfo.InvariantCulture);
                    target.MyoActivationReps = technique.MyoActivationReps;
                    target.MyoRepsPerMiniSet = technique.MyoRepsPerMiniSet;
                    target.MyoRestSeconds = technique.MyoRestSeconds.ToString(CultureInfo.InvariantCulture);
                }

                Exercises.Add(item);
                itemsById[exercise.Id] = item;
                _loadedExerciseIds.Add(exercise.Id);
            }

            foreach (var exercise in exercises)
            {
                if (exercise.SupersetLinkedExerciseId is null)
                    continue;

                if (!itemsById.TryGetValue(exercise.Id, out var current))
                    continue;

                if (!itemsById.TryGetValue(exercise.SupersetLinkedExerciseId.Value, out var partner))
                    continue;

                current.SupersetLinkedLocalKey = partner.LocalKey;
                current.SupersetLinkedExerciseName = partner.ExerciseName;
            }

            ResetExerciseInputs();
            RefreshAvailableSupersetPartners();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar la rutina: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshAvailableSupersetPartners()
    {
        AvailableSupersetPartners.Clear();

        foreach (var exercise in Exercises)
        {
            if (EditingExercise is not null && exercise.LocalKey == EditingExercise.LocalKey)
                continue;

            AvailableSupersetPartners.Add(exercise);
        }
    }

    private void SetSupersetLink(RoutineExerciseEditorItemViewModel exercise, RoutineExerciseEditorItemViewModel? partner)
    {
        if (exercise.SupersetLinkedLocalKey is Guid previousKey)
        {
            var previousPartner = Exercises.FirstOrDefault(x => x.LocalKey == previousKey);
            if (previousPartner is not null && previousPartner.SupersetLinkedLocalKey == exercise.LocalKey)
            {
                previousPartner.SupersetLinkedLocalKey = null;
                previousPartner.SupersetLinkedExerciseName = string.Empty;
            }
        }

        if (partner is null)
        {
            exercise.SupersetLinkedLocalKey = null;
            exercise.SupersetLinkedExerciseName = string.Empty;
            return;
        }

        if (partner.SupersetLinkedLocalKey is Guid partnerPreviousKey)
        {
            var partnerPreviousPartner = Exercises.FirstOrDefault(x => x.LocalKey == partnerPreviousKey);
            if (partnerPreviousPartner is not null && partnerPreviousPartner.SupersetLinkedLocalKey == partner.LocalKey)
            {
                partnerPreviousPartner.SupersetLinkedLocalKey = null;
                partnerPreviousPartner.SupersetLinkedExerciseName = string.Empty;
            }
        }

        exercise.SupersetLinkedLocalKey = partner.LocalKey;
        exercise.SupersetLinkedExerciseName = partner.ExerciseName;
        partner.SupersetLinkedLocalKey = exercise.LocalKey;
        partner.SupersetLinkedExerciseName = exercise.ExerciseName;
    }

    [RelayCommand]
    private void EditExercise(RoutineExerciseEditorItemViewModel? item)
    {
        if (item is null)
            return;

        ErrorMessage = string.Empty;
        EditingExercise = item;

        NewExerciseName = item.ExerciseName;
        NewExerciseBlockType = item.BlockType;
        NewExercisePlannedSets = item.PlannedSets.ToString(CultureInfo.InvariantCulture);
        NewExerciseTargetReps = item.TargetReps;
        NewExerciseSuggestedWeight = item.SuggestedWeight.ToString(CultureInfo.InvariantCulture);
        NewExerciseRestSeconds = item.RestSeconds.ToString(CultureInfo.InvariantCulture);
        NewExerciseNotes = item.Notes;

        NewExerciseSetTechniques.Clear();
        foreach (var technique in item.SetTechniques)
            NewExerciseSetTechniques.Add(technique.Clone());

        NewExerciseIsSuperset = item.IsSupersetLinked;

        RefreshAvailableSupersetPartners();

        NewExerciseSupersetPartner = item.SupersetLinkedLocalKey is Guid linkedKey
            ? Exercises.FirstOrDefault(x => x.LocalKey == linkedKey)
            : null;
    }

    [RelayCommand]
    private void CancelEditExercise()
    {
        EditingExercise = null;
        ResetExerciseInputs();
        RefreshAvailableSupersetPartners();
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void AddExercise()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewExerciseName))
        {
            ErrorMessage = "Introduce un nombre de ejercicio.";
            return;
        }

        if (!int.TryParse(NewExercisePlannedSets, out var plannedSets) || plannedSets <= 0)
        {
            ErrorMessage = "Introduce un número válido de series.";
            return;
        }

        if (!double.TryParse(
                NewExerciseSuggestedWeight.Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var suggestedWeight))
        {
            suggestedWeight = 0;
        }

        if (!int.TryParse(NewExerciseRestSeconds, out var restSeconds) || restSeconds < 0)
        {
            ErrorMessage = "Introduce un descanso válido.";
            return;
        }

        foreach (var technique in NewExerciseSetTechniques)
        {
            if (!ValidateSetTechnique(technique))
                return;
        }

        if (NewExerciseIsSuperset && NewExerciseSupersetPartner is null)
        {
            ErrorMessage = "Selecciona con qué ejercicio forma la superserie.";
            return;
        }

        RoutineExerciseEditorItemViewModel targetItem;

        if (EditingExercise is not null)
        {
            targetItem = EditingExercise;
        }
        else
        {
            targetItem = new RoutineExerciseEditorItemViewModel
            {
                DisplayOrder = Exercises.Count + 1
            };
            Exercises.Add(targetItem);
        }

        targetItem.ExerciseName = NewExerciseName.Trim();
        targetItem.BlockType = string.IsNullOrWhiteSpace(NewExerciseBlockType) ? "Normal" : NewExerciseBlockType.Trim();
        targetItem.PlannedSets = plannedSets;
        targetItem.TargetReps = NewExerciseTargetReps.Trim();
        targetItem.SuggestedWeight = suggestedWeight;
        targetItem.RestSeconds = restSeconds;
        targetItem.Notes = NewExerciseNotes.Trim();

        targetItem.SetTechniques.Clear();
        foreach (var technique in NewExerciseSetTechniques)
            targetItem.SetTechniques.Add(technique.Clone());

        SetSupersetLink(targetItem, NewExerciseIsSuperset ? NewExerciseSupersetPartner : null);

        EditingExercise = null;
        ResetExerciseInputs();
        RefreshAvailableSupersetPartners();
    }

    private bool ValidateSetTechnique(SetTechniqueItemViewModel technique)
    {
        switch (technique.TechniqueType)
        {
            case "Dropset":
                if (string.IsNullOrWhiteSpace(technique.DropWeight) || string.IsNullOrWhiteSpace(technique.DropReps))
                {
                    ErrorMessage = $"Completa el peso y las reps del dropset en la serie {technique.SetIndex}.";
                    return false;
                }
                break;

            case "Dropset doble":
                if (string.IsNullOrWhiteSpace(technique.DropWeight) || string.IsNullOrWhiteSpace(technique.DropReps) ||
                    string.IsNullOrWhiteSpace(technique.DropWeight2) || string.IsNullOrWhiteSpace(technique.DropReps2))
                {
                    ErrorMessage = $"Completa las dos bajadas del dropset doble en la serie {technique.SetIndex}.";
                    return false;
                }
                break;

            case "Cluster":
                if (string.IsNullOrWhiteSpace(technique.ClusterWeight) ||
                    string.IsNullOrWhiteSpace(technique.ClusterRepsPerMiniSet) ||
                    string.IsNullOrWhiteSpace(technique.ClusterMiniSetCount) ||
                    string.IsNullOrWhiteSpace(technique.ClusterRestSeconds))
                {
                    ErrorMessage = $"Completa todos los campos del cluster en la serie {technique.SetIndex}.";
                    return false;
                }
                break;

            case "Myo-reps":
                if (string.IsNullOrWhiteSpace(technique.MyoActivationReps) ||
                    string.IsNullOrWhiteSpace(technique.MyoRepsPerMiniSet) ||
                    string.IsNullOrWhiteSpace(technique.MyoRestSeconds))
                {
                    ErrorMessage = $"Completa todos los campos de myo-reps en la serie {technique.SetIndex}.";
                    return false;
                }
                break;
        }

        return true;
    }

    [RelayCommand]
    private void RemoveExercise(RoutineExerciseEditorItemViewModel? item)
    {
        if (item is null)
            return;

        if (EditingExercise == item)
        {
            EditingExercise = null;
            ResetExerciseInputs();
        }

        SetSupersetLink(item, null);
        Exercises.Remove(item);

        for (int i = 0; i < Exercises.Count; i++)
            Exercises[i].DisplayOrder = i + 1;

        RefreshAvailableSupersetPartners();
    }

    public void ReorderExercises()
    {
        for (var i = 0; i < Exercises.Count; i++)
            Exercises[i].DisplayOrder = i + 1;
    }

    [RelayCommand]
    private async Task SaveRoutineAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(RoutineName))
        {
            ErrorMessage = "Introduce un nombre para la rutina.";
            return;
        }

        if (Exercises.Count == 0)
        {
            ErrorMessage = "Añade al menos un ejercicio.";
            return;
        }

        var activeUser = await _userSessionService.LoadActiveUserAsync();

        if (activeUser is null)
        {
            ErrorMessage = "No hay un usuario activo.";
            return;
        }

        try
        {
            IsBusy = true;

            RoutineTemplate routine;

            if (RoutineId == 0)
            {
                routine = new RoutineTemplate
                {
                    UserProfileId = activeUser.Id,
                    Name = RoutineName.Trim(),
                    Notes = RoutineNotes.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };
            }
            else
            {
                routine = await _database.GetRoutineTemplateByIdAsync(RoutineId)
                    ?? new RoutineTemplate
                    {
                        UserProfileId = activeUser.Id,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                routine.Name = RoutineName.Trim();
                routine.Notes = RoutineNotes.Trim();
            }

            await _database.SaveRoutineTemplateAsync(routine);
            RoutineId = routine.Id;

            var selectedDays = ScheduledDayOptions.Where(x => x.IsSelected).Select(x => x.Day);
            await _database.SetRoutineScheduledDaysAsync(activeUser.Id, routine.Id, selectedDays);

            var currentIds = Exercises.Where(x => x.Id != 0).Select(x => x.Id).ToHashSet();
            var removedIds = _loadedExerciseIds.Except(currentIds).ToList();

            foreach (var removedId in removedIds)
            {
                var stub = new RoutineExercise { Id = removedId };
                await _database.DeleteRoutineExerciseAsync(stub);
            }

            var savedEntities = new Dictionary<Guid, RoutineExercise>();

            foreach (var exercise in Exercises.OrderBy(x => x.DisplayOrder))
            {
                var entity = new RoutineExercise
                {
                    Id = exercise.Id,
                    RoutineTemplateId = routine.Id,
                    UserProfileId = activeUser.Id,
                    DisplayOrder = exercise.DisplayOrder,
                    ExerciseName = exercise.ExerciseName,
                    BlockType = exercise.BlockType,
                    PlannedSets = exercise.PlannedSets,
                    TargetReps = exercise.TargetReps,
                    SuggestedWeight = exercise.SuggestedWeight,
                    RestSeconds = exercise.RestSeconds,
                    Notes = exercise.Notes,
                    SupersetLinkedExerciseId = null
                };

                await _database.SaveRoutineExerciseAsync(entity);
                exercise.Id = entity.Id;
                savedEntities[exercise.LocalKey] = entity;
            }

            foreach (var exercise in Exercises)
            {
                if (exercise.SupersetLinkedLocalKey is not Guid partnerKey)
                    continue;

                if (!savedEntities.TryGetValue(partnerKey, out var partnerEntity))
                    continue;

                var entity = savedEntities[exercise.LocalKey];
                entity.SupersetLinkedExerciseId = partnerEntity.Id;
                await _database.SaveRoutineExerciseAsync(entity);
            }

            foreach (var exercise in Exercises)
            {
                var entity = savedEntities[exercise.LocalKey];

                await _database.DeleteSetTechniquesByExerciseIdAsync(entity.Id);

                foreach (var technique in exercise.SetTechniques.Where(x => x.TechniqueType != "Normal"))
                {
                    await _database.SaveSetTechniqueAsync(new RoutineExerciseSetTechnique
                    {
                        RoutineExerciseId = entity.Id,
                        SetIndex = technique.SetIndex,
                        TechniqueType = technique.TechniqueType,
                        DropWeight = ParseDouble(technique.DropWeight),
                        DropReps = technique.DropReps,
                        DropWeight2 = ParseDouble(technique.DropWeight2),
                        DropReps2 = technique.DropReps2,
                        ClusterWeight = ParseDouble(technique.ClusterWeight),
                        ClusterRepsPerMiniSet = technique.ClusterRepsPerMiniSet,
                        ClusterMiniSetCount = ParseInt(technique.ClusterMiniSetCount),
                        ClusterRestSeconds = ParseInt(technique.ClusterRestSeconds),
                        MyoActivationReps = technique.MyoActivationReps,
                        MyoRepsPerMiniSet = technique.MyoRepsPerMiniSet,
                        MyoRestSeconds = ParseInt(technique.MyoRestSeconds)
                    });
                }
            }

            _loadedExerciseIds = currentIds;

            ErrorMessage = "Rutina guardada correctamente.";
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al guardar la rutina: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static double ParseDouble(string value)
    {
        return double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }

    [RelayCommand]
    private void ToggleScheduledDay(DayToggleItem? item)
    {
        if (item is null)
            return;

        item.IsSelected = !item.IsSelected;
    }

    private void SetSelectedScheduledDays(IEnumerable<int> dayValues)
    {
        var selected = dayValues.ToHashSet();

        foreach (var option in ScheduledDayOptions)
            option.IsSelected = selected.Contains((int)option.Day);
    }

    [RelayCommand]
    private async Task DeleteRoutineAsync()
    {
        if (RoutineId == 0)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var routine = await _database.GetRoutineTemplateByIdAsync(RoutineId);

        if (routine is not null)
            await _database.DeleteRoutineTemplateAsync(routine);

        await Shell.Current.GoToAsync("..");
    }

    private void ResetExerciseInputs()
    {
        NewExerciseName = string.Empty;
        NewExerciseBlockType = "Normal";
        NewExercisePlannedSets = "3";
        NewExerciseTargetReps = "8-12";
        NewExerciseSuggestedWeight = string.Empty;
        NewExerciseRestSeconds = "90";
        NewExerciseNotes = string.Empty;
        NewExerciseIsSuperset = false;
        NewExerciseSupersetPartner = null;
        NewExerciseSetTechniques.Clear();
        SyncNewExerciseSetTechniques(NewExercisePlannedSets);
    }
}