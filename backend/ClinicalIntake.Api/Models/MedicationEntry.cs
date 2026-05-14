namespace ClinicalIntake.Api.Models;

public sealed class MedicationEntry
{
    public int Id { get; set; }
    public int IntakeId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public MedicationCategory Category { get; set; }
    public string? Dose { get; set; }
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public string? ReasonForUse { get; set; }
    public MedicationSource Source { get; set; }
    public string? PrescribedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Intake? Intake { get; set; }
    public List<MedicationSignal> MedicationSignals { get; set; } = [];
}
