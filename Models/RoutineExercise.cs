using SQLite;

namespace IronReign.Models;

public class RoutineExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int RoutineTemplateId { get; set; }

    [Indexed]
    public int UserProfileId { get; set; }

    public int DisplayOrder { get; set; }

    [MaxLength(120)]
    public string ExerciseName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string BlockType { get; set; } = "Normal";

    public int PlannedSets { get; set; }

    [MaxLength(50)]
    public string TargetReps { get; set; } = string.Empty;

    public double SuggestedWeight { get; set; }

    public int RestSeconds { get; set; }

    public string Notes { get; set; } = string.Empty;

    // Enlace de superserie: Id de otro RoutineExercise de la misma rutina, o null si no está enlazado.
    public int? SupersetLinkedExerciseId { get; set; }
}