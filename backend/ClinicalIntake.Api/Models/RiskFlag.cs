namespace ClinicalIntake.Api.Models;

public sealed class RiskFlag
{
    public int Id { get; set; }
    public int IntakeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public RiskSeverity Severity { get; set; }
    public string Reason { get; set; } = string.Empty;

    public Intake? Intake { get; set; }
}
