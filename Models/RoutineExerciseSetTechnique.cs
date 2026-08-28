using SQLite;

namespace IronReign.Models;

public class RoutineExerciseSetTechnique
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int RoutineExerciseId { get; set; }

    // Número de serie dentro del ejercicio (1-based). Ej: 3 = la tercera serie.
    public int SetIndex { get; set; }

    [MaxLength(30)]
    public string TechniqueType { get; set; } = "Normal"; // Normal, Dropset, DropsetDoble, Cluster, MyoReps

    // Dropset
    public double DropWeight { get; set; }

    [MaxLength(20)]
    public string DropReps { get; set; } = string.Empty;

    // Dropset doble (2ª bajada)
    public double DropWeight2 { get; set; }

    [MaxLength(20)]
    public string DropReps2 { get; set; } = string.Empty;

    // Cluster
    public double ClusterWeight { get; set; }

    [MaxLength(20)]
    public string ClusterRepsPerMiniSet { get; set; } = string.Empty;

    public int ClusterMiniSetCount { get; set; }

    public int ClusterRestSeconds { get; set; }

    // Myo-reps
    [MaxLength(20)]
    public string MyoActivationReps { get; set; } = string.Empty;

    [MaxLength(20)]
    public string MyoRepsPerMiniSet { get; set; } = string.Empty;

    public int MyoRestSeconds { get; set; }
}