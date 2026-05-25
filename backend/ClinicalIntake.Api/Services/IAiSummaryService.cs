using ClinicalIntake.Api.Models;

namespace ClinicalIntake.Api.Services;

public sealed record AiSummaryResult(AiSummary Summary, IReadOnlyList<RiskFlag> RiskFlags);

public interface IAiSummaryService
{
    string ProviderName { get; }

    AiSummaryResult Generate(Intake intake);
}
