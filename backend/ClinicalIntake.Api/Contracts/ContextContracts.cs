namespace ClinicalIntake.Api.Contracts;

public sealed record CreateContextEventRequest(
    string SourceType,
    string SourceLabel,
    string Content,
    DateTime? CapturedAt,
    string CreatedBy,
    decimal? ConfidenceScore,
    string? MetadataJson);

public sealed record CreateTranscriptContextRequest(
    string TranscriptLabel,
    string TranscriptText,
    DateTime? CapturedAt,
    string CreatedBy,
    decimal? ConfidenceScore,
    string? SpeakerContext);

public sealed record CreateDocumentContextRequest(
    string DocumentLabel,
    string DocumentText,
    DateTime? CapturedAt,
    string CreatedBy,
    decimal? ConfidenceScore,
    string? DocumentType,
    string? PageReference);

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
