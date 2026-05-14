using ClinicalIntake.Api.Models;
using System.Text.RegularExpressions;

namespace ClinicalIntake.Api.Services;

public sealed class MockAiSummaryService : IAiSummaryService
{
    private static readonly string[] HighRiskTerms =
    [
        "self-harm",
        "self harm",
        "suicidal",
        "harm",
        "abuse",
        "safeguarding"
    ];

    private static readonly string[] UrgencyTerms =
    [
        "urgent",
        "crisis",
        "severe"
    ];

    public AiSummaryResult Generate(Intake intake)
    {
        var text = intake.IntakeText.ToLowerInvariant();
        var concerns = BuildPresentingConcerns(text);
        var riskFlags = BuildRiskFlags(text);
        var highRiskFound = riskFlags.Any(flag => flag.Severity == RiskSeverity.High);
        var mediumRiskFound = riskFlags.Any(flag => flag.Severity == RiskSeverity.Medium);
        var confidenceScore = CalculateConfidenceScore(text, concerns, highRiskFound);

        var summary = new AiSummary
        {
            IntakeId = intake.Id,
            PresentingConcerns = string.Join("; ", concerns),
            RelevantHistory = BuildRelevantHistory(text, intake.Age),
            PossibleRisks = BuildPossibleRisks(riskFlags),
            RecommendedNextStep = BuildRecommendedNextStep(highRiskFound, mediumRiskFound, confidenceScore),
            ConfidenceScore = confidenceScore,
            GeneratedAt = DateTime.UtcNow,
            Disclaimer = AiSafety.Disclaimer
        };

        foreach (var riskFlag in riskFlags)
        {
            riskFlag.IntakeId = intake.Id;
        }

        return new AiSummaryResult(summary, riskFlags);
    }

    private static List<string> BuildPresentingConcerns(string text)
    {
        var concerns = new List<string>();

        AddIfContains(text, "school", "School functioning or attendance concern", concerns);
        AddIfContains(text, "sleep", "Sleep routine or sleep quality concern", concerns);
        AddIfContains(text, "attention", "Attention, focus, or executive functioning concern", concerns);
        AddIfContains(text, "meltdown", "Episodes of distress or emotional regulation concern", concerns);
        AddIfContains(text, "communication", "Communication or social interaction concern", concerns);

        if (concerns.Count == 0)
        {
            concerns.Add("General intake concern described by referrer");
        }

        return concerns;
    }

    private static string BuildRelevantHistory(string text, int age)
    {
        var history = new List<string>
        {
            $"Patient alias is recorded as age {age}. No diagnosis is inferred by this system."
        };

        AddIfContains(text, "family", "Family or caregiver context is mentioned in the intake.", history);
        AddIfContains(text, "previous", "The note mentions previous concerns or prior contact.", history);
        AddIfContains(text, "school", "School context may be relevant for human review.", history);

        return string.Join(" ", history);
    }

    private static List<RiskFlag> BuildRiskFlags(string text)
    {
        var flags = new List<RiskFlag>();

        if (HighRiskTerms.Any(term => ContainsTerm(text, term)))
        {
            flags.Add(new RiskFlag
            {
                Label = "Potential immediate safety or safeguarding concern",
                Severity = RiskSeverity.High,
                Reason = "The intake contains high-priority terms such as self-harm, suicidal, harm, abuse, or safeguarding."
            });
        }

        if (ContainsTerm(text, "crisis"))
        {
            flags.Add(new RiskFlag
            {
                Label = "Crisis language",
                Severity = RiskSeverity.High,
                Reason = "The intake uses crisis language and should be checked promptly by a qualified clinician."
            });
        }
        else if (UrgencyTerms.Any(term => ContainsTerm(text, term)))
        {
            flags.Add(new RiskFlag
            {
                Label = "Urgency language",
                Severity = RiskSeverity.Medium,
                Reason = "The intake includes words such as urgent or severe, which may indicate a higher-priority workflow."
            });
        }

        if (text.Contains("sleep") || text.Contains("meltdown"))
        {
            flags.Add(new RiskFlag
            {
                Label = "Functional impact",
                Severity = RiskSeverity.Low,
                Reason = "Sleep disruption or repeated distress is mentioned and may affect daily functioning."
            });
        }

        return flags;
    }

    private static string BuildPossibleRisks(IReadOnlyCollection<RiskFlag> riskFlags)
    {
        if (riskFlags.Count == 0)
        {
            return "No configured risk keywords were detected. This does not rule out clinical risk.";
        }

        return string.Join("; ", riskFlags.Select(flag => $"{flag.Severity}: {flag.Label}"));
    }

    private static string BuildRecommendedNextStep(bool highRiskFound, bool mediumRiskFound, decimal confidenceScore)
    {
        if (highRiskFound)
        {
            return "Route for urgent human review according to local safeguarding and urgent care policy. No diagnosis or triage decision is made by the software.";
        }

        if (confidenceScore < 0.75m)
        {
            return "Route to human review because the mock summary confidence is below the configured threshold.";
        }

        if (mediumRiskFound)
        {
            return "Route to the care team for prioritised review and context checking.";
        }

        return "Add to routine review workflow for a qualified clinician or care team member to assess.";
    }

    private static decimal CalculateConfidenceScore(string text, IReadOnlyCollection<string> concerns, bool highRiskFound)
    {
        if (text.Length < 80)
        {
            return 0.68m;
        }

        if (highRiskFound)
        {
            return 0.78m;
        }

        if (concerns.Count == 1 && concerns.Contains("General intake concern described by referrer"))
        {
            return 0.70m;
        }

        return 0.86m;
    }

    private static void AddIfContains(string text, string keyword, string value, ICollection<string> values)
    {
        if (ContainsTerm(text, keyword))
        {
            values.Add(value);
        }
    }

    private static bool ContainsTerm(string text, string term)
    {
        var pattern = $@"\b{Regex.Escape(term)}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant);
    }
}
