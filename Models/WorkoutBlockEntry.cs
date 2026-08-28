using SQLite;

namespace IronReign.Models;

public class WorkoutBlockEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int WorkoutExerciseBlockId { get; set; }

    [Indexed]
    public int WorkoutSessionId { get; set; }

    [Indexed]
    public int UserProfileId { get; set; }

    public int EntryOrder { get; set; }

    [MaxLength(50)]
    public string EntryType { get; set; } = "Main";

    public double Weight { get; set; }

    public int Reps { get; set; }

    public int? Rir { get; set; }

    public int RestSeconds { get; set; }

    [MaxLength(200)]
    public string Notes { get; set; } = string.Empty;
}