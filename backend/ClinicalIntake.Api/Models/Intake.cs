namespace ClinicalIntake.Api.Models;

public sealed class Intake
{
    public int Id { get; set; }
    public string PatientAlias { get; set; } = string.Empty;
    public int Age { get; set; }
    public string IntakeText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.New;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public AiSummary? AiSummary { get; set; }
    public List<RiskFlag> RiskFlags { get; set; } = [];
    public List<MedicationEntry> MedicationEntries { get; set; } = [];
    public List<MedicationSignal> MedicationSignals { get; set; } = [];
    public List<AuditLog> AuditLogs { get; set; } = [];
}
