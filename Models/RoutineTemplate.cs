using SQLite;

namespace IronReign.Models;

public class RoutineTemplate
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserProfileId { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int DisplayOrder { get; set; }
}