namespace ClinicalIntake.Api.Models;

public sealed class RiskFlag
{
    public int Id { get; set; }
    public int IntakeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public RiskSeverity Severity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ContextSourceType? EvidenceSourceType { get; set; }
    public string? EvidenceSourceLabel { get; set; }
    public string? EvidenceSnippet { get; set; }

    public Intake? Intake { get; set; }
}
