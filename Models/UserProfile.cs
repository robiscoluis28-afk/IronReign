using SQLite;

namespace IronReign.Models;

public class UserProfile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    [MaxLength(200)]
    public string? FirebaseUid { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string LastName { get; set; } = string.Empty;

    public int Age { get; set; }

    [MaxLength(20)]
    public string Sex { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PreferredWeightUnit { get; set; } = "kg";

    [MaxLength(10)]
    public string PreferredTheme { get; set; } = "dark";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}