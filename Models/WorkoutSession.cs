using SQLite;

namespace IronReign.Models;

public class WorkoutSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserProfileId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime SessionDateUtc { get; set; }

    public int DurationMinutes { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}