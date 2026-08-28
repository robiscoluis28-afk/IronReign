using SQLite;

namespace IronReign.Models;

public class WorkoutExerciseBlock
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public int RoutineExerciseId { get; set; }
    public int DisplayOrder { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string BlockType { get; set; } = "Normal";
    public int PlannedSets { get; set; }
    public string TargetReps { get; set; } = string.Empty;
    public double SuggestedWeight { get; set; }
    public int RestSeconds { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}