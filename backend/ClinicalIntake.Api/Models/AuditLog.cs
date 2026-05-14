namespace ClinicalIntake.Api.Models;

public sealed class AuditLog
{
    public int Id { get; set; }
    public int IntakeId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;

    public Intake? Intake { get; set; }
}
