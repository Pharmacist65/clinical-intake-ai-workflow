namespace ClinicalIntake.Api.Models;

public sealed class MedicationSignal
{
    public int Id { get; set; }
    public int IntakeId { get; set; }
    public int? MedicationEntryId { get; set; }
    public string Label { get; set; } = string.Empty;
    public RiskSeverity Severity { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string ReviewerQuestion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Intake? Intake { get; set; }
    public MedicationEntry? MedicationEntry { get; set; }
}
