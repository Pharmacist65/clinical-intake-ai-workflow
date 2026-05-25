namespace ClinicalIntake.Api.Services;

public sealed class AiSummaryProviderOptions
{
    public const string SectionName = "AiSummary";

    public string Provider { get; init; } = AiSummaryProviderNames.Mock;

    public bool ExternalProvidersEnabled { get; init; }
}
