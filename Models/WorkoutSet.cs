using SQLite;

namespace IronReign.Models;

public class WorkoutSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int WorkoutSessionId { get; set; }

    [Indexed]
    public int UserProfileId { get; set; }

    [MaxLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    public int SetNumber { get; set; }

    public int Reps { get; set; }

    public double Weight { get; set; }

    [MaxLength(50)]
    public string TechniqueType { get; set; } = "Normal";

    [MaxLength(300)]
    public string Notes { get; set; } = string.Empty;
}