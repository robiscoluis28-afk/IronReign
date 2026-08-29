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

    // Días de la semana asignados a esta rutina, como valores de DayOfWeek
    // separados por coma (0=domingo..6=sábado). Ej. "1,4" = lunes y jueves.
    [MaxLength(20)]
    public string ScheduledDays { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int DisplayOrder { get; set; }
}