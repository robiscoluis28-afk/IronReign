using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Models;
using System.Globalization;

namespace IronReign.ViewModels;

public readonly record struct CompletedSetEntry(double Weight, int Reps, int RestSeconds, string EntryType, int? Rir);

public partial class WorkoutExerciseRunItemViewModel : ObservableObject
{
    private int _subStepIndex;
    private int _myoMiniSetCount;

    [ObservableProperty]
    private int routineExerciseId;

    [ObservableProperty]
    private string exerciseName = string.Empty;

    [ObservableProperty]
    private string blockType = "Normal";

    [ObservableProperty]
    private string targetRepsRaw = string.Empty;

    [ObservableProperty]
    private int totalSets;

    [ObservableProperty]
    private int completedSets;

    [ObservableProperty]
    private string setsProgress = "0/0 series";

    [ObservableProperty]
    private double progressRatio;

    [ObservableProperty]
    private string targetReps = string.Empty;

    [ObservableProperty]
    private string lastPerformance = string.Empty;

    [ObservableProperty]
    private string weightText = string.Empty;

    [ObservableProperty]
    private string repsText = string.Empty;

    [ObservableProperty]
    private string rirText = string.Empty;

    [ObservableProperty]
    private bool isWorkoutActive;

    [ObservableProperty]
    private bool isRestActive;

    [ObservableProperty]
    private int remainingRestSeconds;

    [ObservableProperty]
    private int plannedRestSeconds = 90;

    [ObservableProperty]
    private string currentStepLabel = "Serie 1";

    [ObservableProperty]
    private string currentStepInstruction = string.Empty;

    [ObservableProperty]
    private bool isMyoDecisionActive;

    public List<RoutineExerciseSetTechnique> SetTechniques { get; set; } = new();

    public List<CompletedSetEntry> CompletedEntries { get; } = new();

    partial void OnTotalSetsChanged(int value) => RefreshProgress();
    partial void OnCompletedSetsChanged(int value) => RefreshProgress();

    public void RefreshProgress()
    {
        SetsProgress = $"{CompletedSets}/{TotalSets} series";
        ProgressRatio = TotalSets > 0 ? (double)CompletedSets / TotalSets : 0;
    }

    private RoutineExerciseSetTechnique? GetCurrentTechnique()
    {
        var setIndex = CompletedSets + 1;
        return SetTechniques.FirstOrDefault(t => t.SetIndex == setIndex && t.TechniqueType != "Normal");
    }

    public void RefreshCurrentStep()
    {
        IsMyoDecisionActive = false;
        var technique = GetCurrentTechnique();

        if (technique is null)
        {
            CurrentStepLabel = $"Serie {Math.Min(CompletedSets + 1, TotalSets)} de {TotalSets}";
            CurrentStepInstruction = string.Empty;
            return;
        }

        switch (technique.TechniqueType)
        {
            case "Dropset":
                if (_subStepIndex == 0)
                {
                    CurrentStepLabel = $"Serie {CompletedSets + 1} · Principal";
                    CurrentStepInstruction = "Haz la serie principal; después bajarás el peso.";
                }
                else
                {
                    CurrentStepLabel = $"Serie {CompletedSets + 1} · Bajada";
                    CurrentStepInstruction = $"Baja a {technique.DropWeight:0.##} kg y busca {technique.DropReps} reps.";
                    WeightText = technique.DropWeight.ToString("0.##", CultureInfo.InvariantCulture);
                    RepsText = technique.DropReps;
                }
                break;

            case "Dropset doble":
                if (_subStepIndex == 0)
                {
                    CurrentStepLabel = $"Serie {CompletedSets + 1} · Principal";
                    CurrentStepInstruction = "Haz la serie principal; después harás dos bajadas.";
                }
                else if (_subStepIndex == 1)
                {
                    CurrentStepLabel = $"Serie {CompletedSets + 1} · 1ª bajada";
                    CurrentStepInstruction = $"Baja a {technique.DropWeight:0.##} kg y busca {technique.DropReps} reps.";
                    WeightText = technique.DropWeight.ToString("0.##", CultureInfo.InvariantCulture);
                    RepsText = technique.DropReps;
                }
                else
                {
                    CurrentStepLabel = $"Serie {CompletedSets + 1} · 2ª bajada";
                    CurrentStepInstruction = $"Baja a {technique.DropWeight2:0.##} kg y busca {technique.DropReps2} reps.";
                    WeightText = technique.DropWeight2.ToString("0.##", CultureInfo.InvariantCulture);
                    RepsText = technique.DropReps2;
                }
                break;

            case "Cluster":
                CurrentStepLabel = $"Serie {CompletedSets + 1} · Mini-serie {_subStepIndex + 1} de {technique.ClusterMiniSetCount}";
                CurrentStepInstruction = $"{technique.ClusterWeight:0.##} kg x {technique.ClusterRepsPerMiniSet} reps. Descanso {technique.ClusterRestSeconds}s entre mini-series.";
                WeightText = technique.ClusterWeight.ToString("0.##", CultureInfo.InvariantCulture);
                RepsText = technique.ClusterRepsPerMiniSet;
                break;

            case "Myo-reps":
                if (_subStepIndex == 0)
                {
                    CurrentStepLabel = $"Serie {CompletedSets + 1} · Activación";
                    CurrentStepInstruction = $"Serie de activación: busca {technique.MyoActivationReps} reps.";
                    RepsText = technique.MyoActivationReps;
                }
                else
                {
                    CurrentStepLabel = $"Serie {CompletedSets + 1} · Mini-serie {_myoMiniSetCount + 1}";
                    CurrentStepInstruction = $"Busca {technique.MyoRepsPerMiniSet} reps al mismo peso. Descansa {technique.MyoRestSeconds}s y decide si continúas.";
                    RepsText = technique.MyoRepsPerMiniSet;
                    IsMyoDecisionActive = true;
                }
                break;

            default:
                CurrentStepLabel = $"Serie {CompletedSets + 1} de {TotalSets}";
                CurrentStepInstruction = string.Empty;
                break;
        }
    }

    [RelayCommand]
    private void CompleteSetFromExercise()
    {
        if (!IsWorkoutActive || IsMyoDecisionActive)
            return;

        if (CompletedSets >= TotalSets)
            return;

        var weight = ParseWeight(WeightText);
        var reps = ParseReps(RepsText);
        var rir = ParseRir(RirText);
        var technique = GetCurrentTechnique();

        if (technique is null)
        {
            CompletedEntries.Add(new CompletedSetEntry(weight, reps, PlannedRestSeconds, "Main", rir));
            AdvanceToNextPlannedSet();
            return;
        }

        switch (technique.TechniqueType)
        {
            case "Dropset":
                if (_subStepIndex == 0)
                {
                    CompletedEntries.Add(new CompletedSetEntry(weight, reps, 0, "Main", rir));
                    _subStepIndex = 1;
                    RirText = string.Empty;
                    RefreshCurrentStep();
                }
                else
                {
                    CompletedEntries.Add(new CompletedSetEntry(weight, reps, 0, "Drop 1", rir));
                    _subStepIndex = 0;
                    AdvanceToNextPlannedSet();
                }
                break;

            case "Dropset doble":
                if (_subStepIndex == 0)
                {
                    CompletedEntries.Add(new CompletedSetEntry(weight, reps, 0, "Main", rir));
                    _subStepIndex = 1;
                    RirText = string.Empty;
                    RefreshCurrentStep();
                }
                else if (_subStepIndex == 1)
                {
                    CompletedEntries.Add(new CompletedSetEntry(weight, reps, 0, "Drop 1", rir));
                    _subStepIndex = 2;
                    RirText = string.Empty;
                    RefreshCurrentStep();
                }
                else
                {
                    CompletedEntries.Add(new CompletedSetEntry(weight, reps, 0, "Drop 2", rir));
                    _subStepIndex = 0;
                    AdvanceToNextPlannedSet();
                }
                break;

            case "Cluster":
                CompletedEntries.Add(new CompletedSetEntry(weight, reps, technique.ClusterRestSeconds, $"Cluster mini {_subStepIndex + 1}", rir));
                _subStepIndex++;
                RirText = string.Empty;

                if (_subStepIndex >= technique.ClusterMiniSetCount)
                {
                    _subStepIndex = 0;
                    AdvanceToNextPlannedSet();
                }
                else
                {
                    RefreshCurrentStep();
                    RemainingRestSeconds = technique.ClusterRestSeconds > 0 ? technique.ClusterRestSeconds : 15;
                    IsRestActive = true;
                }
                break;

            case "Myo-reps":
                CompletedEntries.Add(new CompletedSetEntry(weight, reps, technique.MyoRestSeconds, "Activation", rir));
                _subStepIndex = 1;
                _myoMiniSetCount = 0;
                RirText = string.Empty;
                RefreshCurrentStep();
                RemainingRestSeconds = technique.MyoRestSeconds > 0 ? technique.MyoRestSeconds : 15;
                IsRestActive = true;
                break;

            default:
                CompletedEntries.Add(new CompletedSetEntry(weight, reps, PlannedRestSeconds, "Main", rir));
                AdvanceToNextPlannedSet();
                break;
        }

        RefreshProgress();
    }

    [RelayCommand]
    private void AddMyoMiniSet()
    {
        if (!IsWorkoutActive || !IsMyoDecisionActive)
            return;

        var technique = GetCurrentTechnique();
        if (technique is null)
            return;

        var weight = ParseWeight(WeightText);
        var reps = ParseReps(RepsText);
        var rir = ParseRir(RirText);

        _myoMiniSetCount++;
        CompletedEntries.Add(new CompletedSetEntry(weight, reps, technique.MyoRestSeconds, $"Mini {_myoMiniSetCount}", rir));
        RirText = string.Empty;

        RefreshCurrentStep();
        RemainingRestSeconds = technique.MyoRestSeconds > 0 ? technique.MyoRestSeconds : 15;
        IsRestActive = true;
    }

    [RelayCommand]
    private void FinishMyoTechnique()
    {
        if (!IsWorkoutActive || !IsMyoDecisionActive)
            return;

        _subStepIndex = 0;
        AdvanceToNextPlannedSet();
    }

    private void AdvanceToNextPlannedSet()
    {
        CompletedSets++;
        _subStepIndex = 0;
        _myoMiniSetCount = 0;
        WeightText = string.Empty;
        RepsText = string.Empty;
        RirText = string.Empty;

        if (CompletedSets < TotalSets)
        {
            RefreshCurrentStep();
            StartExerciseRest();
        }
        else
        {
            IsRestActive = false;
            RemainingRestSeconds = 0;
            IsMyoDecisionActive = false;
        }

        RefreshProgress();
    }

    [RelayCommand]
    private void DecreaseSetFromExercise()
    {
        if (CompletedSets <= 0)
            return;

        CompletedSets--;
        _subStepIndex = 0;
        _myoMiniSetCount = 0;

        if (CompletedEntries.Count > 0)
            CompletedEntries.RemoveAt(CompletedEntries.Count - 1);

        RefreshCurrentStep();
        RefreshProgress();
    }

    public void ResetForNewWorkout()
    {
        CompletedSets = 0;
        _subStepIndex = 0;
        _myoMiniSetCount = 0;
        CompletedEntries.Clear();
        WeightText = string.Empty;
        RepsText = string.Empty;
        RirText = string.Empty;
        IsRestActive = false;
        IsMyoDecisionActive = false;
        RemainingRestSeconds = 0;
        RefreshCurrentStep();
        RefreshProgress();
    }

    private static double ParseWeight(string text) =>
        double.TryParse(text?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static int ParseReps(string text) => int.TryParse(text, out var value) ? value : 0;

    private static int? ParseRir(string text) => int.TryParse(text, out var value) ? value : null;

    public void StartExerciseRest()
    {
        RemainingRestSeconds = PlannedRestSeconds > 0
            ? PlannedRestSeconds
            : 90;

        IsRestActive = true;
    }

    public void SkipExerciseRest()
    {
        RemainingRestSeconds = 0;
        IsRestActive = false;
    }

    [RelayCommand]
    private void SkipRestFromExercise()
    {
        SkipExerciseRest();
    }

    public void Tick()
    {
        if (!IsRestActive)
            return;

        if (RemainingRestSeconds <= 1)
        {
            RemainingRestSeconds = 0;
            IsRestActive = false;
            return;
        }

        RemainingRestSeconds--;
    }
}
