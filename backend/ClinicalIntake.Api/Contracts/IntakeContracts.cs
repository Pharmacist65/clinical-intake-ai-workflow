namespace ClinicalIntake.Api.Contracts;

public sealed record CreateIntakeRequest(
    string PatientAlias,
    int Age,
    string IntakeText,
    string Source,
    string CreatedBy);

public sealed record UpdateReviewStatusRequest(
    string ReviewStatus,
    string Actor,
    string? ReviewNote = null);

public sealed record IntakeListItemResponse(
    int Id,
    string PatientAlias,
    int Age,
    string Source,
    string ReviewStatus,
    DateTime CreatedAt,
    string CreatedBy,
    string? HighestRiskSeverity);

public sealed record IntakeDetailResponse(
    int Id,
    string PatientAlias,
    int Age,
    string IntakeText,
    string Source,
    string ReviewStatus,
    DateTime CreatedAt,
    string CreatedBy,
    AiSummaryResponse? AiSummary,
    IReadOnlyList<RiskFlagResponse> RiskFlags,
    IReadOnlyList<ContextEventResponse> ContextEvents,
    IReadOnlyList<MedicationEntryResponse> MedicationEntries,
    IReadOnlyList<MedicationSignalResponse> MedicationSignals,
    MedicationDocumentationQualityResponse MedicationDocumentationQuality,
    IReadOnlyList<AuditLogResponse> AuditLogs);

public sealed record AiSummaryResponse(
    int Id,
    int IntakeId,
    string PresentingConcerns,
    string RelevantHistory,
    string PossibleRisks,
    string RecommendedNextStep,
    decimal ConfidenceScore,
    DateTime GeneratedAt,
    string Disclaimer);

public sealed record RiskFlagResponse(
    int Id,
    int IntakeId,
    string Label,
    string Severity,
    string Reason);

public sealed record ReviewQueueItemResponse(
    int Id,
    string PatientAlias,
    int Age,
    string Source,
    DateTime CreatedAt,
    string HighestRiskSeverity,
    IReadOnlyList<RiskFlagResponse> RiskFlags);

public sealed record AuditLogResponse(
    int Id,
    int IntakeId,
    string Action,
    string Actor,
    DateTime Timestamp,
    string Details);
