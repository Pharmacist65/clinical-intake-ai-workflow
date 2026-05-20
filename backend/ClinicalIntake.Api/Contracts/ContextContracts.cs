namespace ClinicalIntake.Api.Contracts;

public sealed record CreateContextEventRequest(
    string SourceType,
    string SourceLabel,
    string Content,
    DateTime? CapturedAt,
    string CreatedBy,
    decimal? ConfidenceScore,
    string? MetadataJson);

public sealed record ContextEventResponse(
    int Id,
    int IntakeId,
    string SourceType,
    string SourceLabel,
    string Content,
    DateTime CapturedAt,
    string CreatedBy,
    decimal? ConfidenceScore,
    string? MetadataJson,
    DateTime CreatedAt);
