using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace IronReign.ViewModels;

public partial class RoutineExerciseEditorItemViewModel : ObservableObject
{
    public Guid LocalKey { get; } = Guid.NewGuid();

    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial int DisplayOrder { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BlockType { get; set; } = "Normal";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial int PlannedSets { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string TargetReps { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial double SuggestedWeight { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial int RestSeconds { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSupersetLinked))]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial Guid? SupersetLinkedLocalKey { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string SupersetLinkedExerciseName { get; set; } = string.Empty;

    public bool IsSupersetLinked => SupersetLinkedLocalKey is not null;

    public ObservableCollection<SetTechniqueItemViewModel> SetTechniques { get; } = new();

    public string Summary
    {
        get
        {
            var baseSummary =
                $"{PlannedSets} series · {TargetReps} reps · " +
                $"{SuggestedWeight:0.##} kg · descanso {RestSeconds}s";

            var specialSets = SetTechniques.Count(x => x.TechniqueType != "Normal");
            if (specialSets > 0)
                baseSummary += $" · {specialSets} serie(s) con técnica";

            if (IsSupersetLinked)
                baseSummary += $" · 🔗 {SupersetLinkedExerciseName}";

            return baseSummary;
        }
    }

    public void SyncSetTechniqueCount(int plannedSets)
    {
        while (SetTechniques.Count < plannedSets)
        {
            SetTechniques.Add(new SetTechniqueItemViewModel
            {
                SetIndex = SetTechniques.Count + 1
            });
        }

        while (SetTechniques.Count > plannedSets)
        {
            SetTechniques.RemoveAt(SetTechniques.Count - 1);
        }
    }
}