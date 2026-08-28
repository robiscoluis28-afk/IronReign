using CommunityToolkit.Mvvm.ComponentModel;

namespace IronReign.ViewModels;

public partial class SetTechniqueItemViewModel : ObservableObject
{
    public int SetIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDropset))]
    [NotifyPropertyChangedFor(nameof(IsDropsetDoble))]
    [NotifyPropertyChangedFor(nameof(IsCluster))]
    [NotifyPropertyChangedFor(nameof(IsMyoReps))]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string TechniqueType { get; set; } = "Normal";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string DropWeight { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string DropReps { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string DropWeight2 { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string DropReps2 { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string ClusterWeight { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string ClusterRepsPerMiniSet { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string ClusterMiniSetCount { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string ClusterRestSeconds { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string MyoActivationReps { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string MyoRepsPerMiniSet { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string MyoRestSeconds { get; set; } = string.Empty;

    public bool IsDropset => TechniqueType == "Dropset";
    public bool IsDropsetDoble => TechniqueType == "Dropset doble";
    public bool IsCluster => TechniqueType == "Cluster";
    public bool IsMyoReps => TechniqueType == "Myo-reps";

    public string Summary => TechniqueType switch
    {
        "Dropset" => $"Serie {SetIndex}: Dropset → {DropWeight} kg x {DropReps}",
        "Dropset doble" => $"Serie {SetIndex}: Dropset doble → {DropWeight} kg x {DropReps} → {DropWeight2} kg x {DropReps2}",
        "Cluster" => $"Serie {SetIndex}: Cluster → {ClusterWeight} kg, {ClusterMiniSetCount} x {ClusterRepsPerMiniSet}, descanso {ClusterRestSeconds}s",
        "Myo-reps" => $"Serie {SetIndex}: Myo-reps → activación {MyoActivationReps}, mini-series de {MyoRepsPerMiniSet}, descanso {MyoRestSeconds}s",
        _ => $"Serie {SetIndex}: Normal"
    };

    public SetTechniqueItemViewModel Clone()
    {
        return new SetTechniqueItemViewModel
        {
            SetIndex = SetIndex,
            TechniqueType = TechniqueType,
            DropWeight = DropWeight,
            DropReps = DropReps,
            DropWeight2 = DropWeight2,
            DropReps2 = DropReps2,
            ClusterWeight = ClusterWeight,
            ClusterRepsPerMiniSet = ClusterRepsPerMiniSet,
            ClusterMiniSetCount = ClusterMiniSetCount,
            ClusterRestSeconds = ClusterRestSeconds,
            MyoActivationReps = MyoActivationReps,
            MyoRepsPerMiniSet = MyoRepsPerMiniSet,
            MyoRestSeconds = MyoRestSeconds
        };
    }
}