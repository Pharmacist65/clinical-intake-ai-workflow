namespace ClinicalIntake.Api.Models;

public sealed class AiSummary
{
    public int Id { get; set; }
    public int IntakeId { get; set; }
    public string PresentingConcerns { get; set; } = string.Empty;
    public string RelevantHistory { get; set; } = string.Empty;
    public string PossibleRisks { get; set; } = string.Empty;
    public string RecommendedNextStep { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Disclaimer { get; set; } = AiSafety.Disclaimer;

    public Intake? Intake { get; set; }
}
