namespace ClinicalIntake.Api.Contracts;

public sealed record CreateMedicationEntryRequest(
    string MedicationName,
    string Category,
    string? Dose,
    string? Route,
    string? Frequency,
    DateTime? StartedAt,
    DateTime? StoppedAt,
    string? ReasonForUse,
    string Source,
    string? PrescribedBy,
    string? Notes);

public sealed record MedicationEntryResponse(
    int Id,
    int IntakeId,
    string MedicationName,
    string NormalizedName,
    string Category,
    string? Dose,
    string? Route,
    string? Frequency,
    DateTime? StartedAt,
    DateTime? StoppedAt,
    string? ReasonForUse,
    string Source,
    string? PrescribedBy,
    string? Notes,
    DateTime CreatedAt);

public sealed record MedicationSignalResponse(
    int Id,
    int IntakeId,
    int? MedicationEntryId,
    string Label,
    string Severity,
    string Rationale,
    string ReviewerQuestion,
    DateTime CreatedAt);
