namespace ClinicalIntake.Api.Models;

public sealed class ContextEvent
{
    public int Id { get; set; }
    public int IntakeId { get; set; }
    public ContextSourceType SourceType { get; set; }
    public string SourceLabel { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }

    public Intake? Intake { get; set; }
}
