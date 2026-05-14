using ClinicalIntake.Api.Models;

namespace ClinicalIntake.Api.Services;

public sealed record AiSummaryResult(AiSummary Summary, IReadOnlyList<RiskFlag> RiskFlags);

public interface IAiSummaryService
{
    AiSummaryResult Generate(Intake intake);
}
