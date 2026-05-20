using System.Text.RegularExpressions;
using ClinicalIntake.Api.Models;

namespace ClinicalIntake.Api.Services;

public sealed class MedicationContextService
{
    public const string DocumentationQualityDisclaimer =
        "Medication documentation quality reflects completeness of captured medication-history fields only. It is not a clinical risk score, diagnosis, prescribing recommendation, medication reconciliation, drug-interaction check, or clinical decision support.";

    private const int EvidenceSnippetRadius = 70;

    private static readonly string[] NsaidTerms =
    [
        "ibuprofen",
        "nurofen",
        "naproxen",
        "nsaid",
        "nsaids"
    ];

    private static readonly string[] SafetyContextTerms =
    [
        "asthma",
        "kidney",
        "liver",
        "stomach ulcer",
        "bleeding",
        "anticoagulant",
        "warfarin",
        "apixaban",
        "rivaroxaban",
        "steroid"
    ];

    private static readonly string[] AdverseReactionTerms =
    [
        "allergy",
        "rash",
        "swelling",
        "breathing difficulty",
        "reaction",
        "side effect"
    ];

    public IReadOnlyList<MedicationSignal> Analyse(Intake intake)
    {
        var now = DateTime.UtcNow;
        var signals = new List<MedicationSignal>();
        var nsaidEntries = intake.MedicationEntries
            .Where(medication => ContainsAny(medication.MedicationName, NsaidTerms))
            .ToList();

        foreach (var medication in nsaidEntries)
        {
            signals.Add(CreateSignal(
                intake.Id,
                medication.Id,
                "OTC NSAID context",
                RiskSeverity.Medium,
                "Medication name includes an NSAID/OTC NSAID term. This is a workflow support signal only.",
                "Confirm dose, duration, reason for use, and whether a pharmacist/clinician review is needed.",
                now,
                ContextSourceType.MedicationHistory,
                medication.MedicationName,
                FindEvidenceSnippet(BuildMedicationEvidenceText(medication), NsaidTerms) ?? BuildMedicationEvidenceText(medication)));
        }

        AddMedicationSafetySignalIfNeeded(intake, nsaidEntries, signals, now);
        AddIncompleteHistorySignals(intake, signals, now);
        AddPolypharmacySignalIfNeeded(intake, signals, now);
        AddHouseholdMedicationSignals(intake, signals, now);
        AddAdverseReactionSignals(intake, signals, now);

        return signals;
    }

    public static MedicationDocumentationQuality AssessDocumentationQuality(
        IReadOnlyCollection<MedicationEntry> medications)
    {
        if (medications.Count == 0)
        {
            return new MedicationDocumentationQuality(
                null,
                "NotAssessed",
                "No medication entries have been recorded for this intake.",
                [],
                DocumentationQualityDisclaimer);
        }

        var issues = new List<MedicationDocumentationIssue>();
        var entryScores = medications
            .Select(medication => AssessMedicationEntry(medication, issues))
            .ToList();
        var score = (int)Math.Round(entryScores.Average(), MidpointRounding.AwayFromZero);
        var status = score switch
        {
            >= 85 => "WellDocumented",
            >= 65 => "NeedsClarification",
            _ => "Incomplete"
        };

        var summary = status switch
        {
            "WellDocumented" => "Medication context is mostly complete for workflow review.",
            "NeedsClarification" => "Some medication-history fields should be clarified before or during human review.",
            _ => "Medication context has important documentation gaps that should be clarified by a human reviewer."
        };

        return new MedicationDocumentationQuality(
            score,
            status,
            summary,
            issues
                .OrderBy(issue => issue.MedicationName)
                .ThenBy(issue => issue.Field)
                .ToList(),
            DocumentationQualityDisclaimer);
    }

    private static void AddMedicationSafetySignalIfNeeded(
        Intake intake,
        IReadOnlyList<MedicationEntry> nsaidEntries,
        ICollection<MedicationSignal> signals,
        DateTime now)
    {
        var nsaidContextEntry = nsaidEntries.FirstOrDefault()
            ?? intake.MedicationEntries.FirstOrDefault(medication => ContainsAny(medication.Notes ?? string.Empty, NsaidTerms));
        var nsaidMentioned = nsaidContextEntry is not null || ContainsAny(intake.IntakeText, NsaidTerms);
        if (!nsaidMentioned)
        {
            return;
        }

        var contextText = string.Join(
            " ",
            intake.IntakeText,
            string.Join(" ", intake.MedicationEntries.Select(medication => medication.Notes ?? string.Empty)));

        if (!ContainsAny(contextText, SafetyContextTerms))
        {
            return;
        }

        signals.Add(CreateSignal(
            intake.Id,
            nsaidContextEntry?.Id,
            "Medication safety review signal",
            RiskSeverity.High,
            "NSAID context appears alongside medical or medication-history terms that should be clarified by a qualified professional. This does not infer causality.",
            "Review NSAID context and relevant medical/medication history with a qualified clinician or pharmacist.",
            now,
            nsaidContextEntry is null ? ContextSourceType.IntakeText : ContextSourceType.MedicationHistory,
            nsaidContextEntry?.MedicationName ?? "Original intake text",
            FindEvidenceSnippet(contextText, NsaidTerms.Concat(SafetyContextTerms)) ?? TrimEvidenceSnippet(contextText)));
    }

    private static void AddIncompleteHistorySignals(
        Intake intake,
        ICollection<MedicationSignal> signals,
        DateTime now)
    {
        foreach (var medication in intake.MedicationEntries
            .Where(medication => medication.Category is MedicationCategory.Current or MedicationCategory.Recent)
            .Where(medication => string.IsNullOrWhiteSpace(medication.Dose) || string.IsNullOrWhiteSpace(medication.Frequency)))
        {
            signals.Add(CreateSignal(
                intake.Id,
                medication.Id,
                "Incomplete medication history",
                RiskSeverity.Low,
                "A current or recent medication is missing dose or frequency documentation.",
                "Clarify dose, frequency, timing, and whether the medication is current or historical.",
                now,
                ContextSourceType.MedicationHistory,
                medication.MedicationName,
                BuildMedicationDocumentationSnippet(medication)));
        }
    }

    private static void AddPolypharmacySignalIfNeeded(
        Intake intake,
        ICollection<MedicationSignal> signals,
        DateTime now)
    {
        var currentMedicationCount = intake.MedicationEntries.Count(medication => medication.Category == MedicationCategory.Current);
        if (currentMedicationCount < 5)
        {
            return;
        }

        signals.Add(CreateSignal(
            intake.Id,
            null,
            "Polypharmacy context",
            RiskSeverity.Medium,
            "Five or more current medications are documented. This is a documentation and review signal only.",
            "Consider pharmacist review of current medication list and documentation quality.",
            now,
            ContextSourceType.MedicationHistory,
            "Current medication list",
            $"Current medications documented: {string.Join(", ", intake.MedicationEntries.Where(medication => medication.Category == MedicationCategory.Current).Select(medication => medication.MedicationName).Take(8))}."));
    }

    private static void AddHouseholdMedicationSignals(
        Intake intake,
        ICollection<MedicationSignal> signals,
        DateTime now)
    {
        if (intake.Age >= 18)
        {
            return;
        }

        foreach (var medication in intake.MedicationEntries
            .Where(medication => medication.Category == MedicationCategory.FamilyHousehold))
        {
            signals.Add(CreateSignal(
                intake.Id,
                medication.Id,
                "Household medication context",
                RiskSeverity.Low,
                "Medication is documented as family or household context for a child intake.",
                "Clarify whether the medication belongs to the patient, caregiver, or household only.",
                now,
                ContextSourceType.MedicationHistory,
                medication.MedicationName,
                BuildMedicationEvidenceText(medication)));
        }
    }

    private static void AddAdverseReactionSignals(
        Intake intake,
        ICollection<MedicationSignal> signals,
        DateTime now)
    {
        foreach (var medication in intake.MedicationEntries
            .Where(medication => ContainsAny(medication.Notes ?? string.Empty, AdverseReactionTerms)))
        {
            signals.Add(CreateSignal(
                intake.Id,
                medication.Id,
                "Possible adverse reaction history",
                RiskSeverity.High,
                "Medication notes include allergy, reaction, or possible adverse-effect language that should be clarified. This does not diagnose an allergy.",
                "Clarify allergy/adverse reaction history and ensure clinician/pharmacist review.",
                now,
                ContextSourceType.MedicationHistory,
                medication.MedicationName,
                FindEvidenceSnippet(BuildMedicationEvidenceText(medication), AdverseReactionTerms) ?? BuildMedicationEvidenceText(medication)));
        }
    }

    private static MedicationSignal CreateSignal(
        int intakeId,
        int? medicationEntryId,
        string label,
        RiskSeverity severity,
        string rationale,
        string reviewerQuestion,
        DateTime createdAt,
        ContextSourceType? evidenceSourceType = null,
        string? evidenceSourceLabel = null,
        string? evidenceSnippet = null) =>
        new()
        {
            IntakeId = intakeId,
            MedicationEntryId = medicationEntryId,
            Label = label,
            Severity = severity,
            Rationale = rationale,
            ReviewerQuestion = reviewerQuestion,
            EvidenceSourceType = evidenceSourceType,
            EvidenceSourceLabel = evidenceSourceLabel,
            EvidenceSnippet = evidenceSnippet,
            CreatedAt = createdAt
        };

    private static int AssessMedicationEntry(
        MedicationEntry medication,
        ICollection<MedicationDocumentationIssue> issues)
    {
        var penalty = 0;

        penalty += AddIssueIf(
            issues,
            medication,
            medication.Source == MedicationSource.Unknown,
            "source",
            "Medication source is unknown.",
            10);

        penalty += AddIssueIf(
            issues,
            medication,
            string.IsNullOrWhiteSpace(medication.Route),
            "route",
            "Route is not documented.",
            5);

        penalty += AddIssueIf(
            issues,
            medication,
            string.IsNullOrWhiteSpace(medication.ReasonForUse),
            "reasonForUse",
            "Reason for use is not documented.",
            5);

        if (medication.Category is MedicationCategory.Current or MedicationCategory.Recent)
        {
            penalty += AddIssueIf(
                issues,
                medication,
                string.IsNullOrWhiteSpace(medication.Dose),
                "dose",
                "Dose is missing for a current or recent medication.",
                20);

            penalty += AddIssueIf(
                issues,
                medication,
                string.IsNullOrWhiteSpace(medication.Frequency),
                "frequency",
                "Frequency is missing for a current or recent medication.",
                20);

            penalty += AddIssueIf(
                issues,
                medication,
                medication.StartedAt is null && medication.StoppedAt is null,
                "timing",
                "Medication timing is not documented.",
                10);
        }

        if (medication.Category == MedicationCategory.OTC)
        {
            penalty += AddIssueIf(
                issues,
                medication,
                string.IsNullOrWhiteSpace(medication.Dose),
                "dose",
                "Dose is not documented for OTC medication context.",
                10);

            penalty += AddIssueIf(
                issues,
                medication,
                string.IsNullOrWhiteSpace(medication.Frequency),
                "frequency",
                "Frequency is not documented for OTC medication context.",
                10);
        }

        if (medication.Category == MedicationCategory.FamilyHousehold)
        {
            penalty += AddIssueIf(
                issues,
                medication,
                string.IsNullOrWhiteSpace(medication.Notes),
                "ownershipContext",
                "Household or family medication ownership context is not documented.",
                10);
        }

        return Math.Max(0, 100 - penalty);
    }

    private static int AddIssueIf(
        ICollection<MedicationDocumentationIssue> issues,
        MedicationEntry medication,
        bool condition,
        string field,
        string reason,
        int penalty)
    {
        if (!condition)
        {
            return 0;
        }

        issues.Add(new MedicationDocumentationIssue(
            medication.Id,
            medication.MedicationName,
            field,
            reason));
        return penalty;
    }

    private static string BuildMedicationEvidenceText(MedicationEntry medication)
    {
        var parts = new[]
        {
            medication.MedicationName,
            medication.Category.ToString(),
            medication.Dose,
            medication.Frequency,
            medication.Route,
            medication.ReasonForUse,
            medication.Notes
        };

        return TrimEvidenceSnippet(string.Join(" | ", parts.Where(part => !string.IsNullOrWhiteSpace(part))));
    }

    private static string BuildMedicationDocumentationSnippet(MedicationEntry medication)
    {
        var dose = string.IsNullOrWhiteSpace(medication.Dose) ? "dose not documented" : $"dose {medication.Dose}";
        var frequency = string.IsNullOrWhiteSpace(medication.Frequency)
            ? "frequency not documented"
            : $"frequency {medication.Frequency}";

        return TrimEvidenceSnippet($"{medication.MedicationName} | {medication.Category} | {dose} | {frequency}");
    }

    private static string? FindEvidenceSnippet(string text, IEnumerable<string> terms)
    {
        foreach (var term in terms)
        {
            var pattern = $@"\b{Regex.Escape(term)}\b";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                return BuildSnippet(text, match.Index, match.Length);
            }
        }

        return null;
    }

    private static string BuildSnippet(string text, int matchIndex, int matchLength)
    {
        var start = Math.Max(0, matchIndex - EvidenceSnippetRadius);
        var end = Math.Min(text.Length, matchIndex + matchLength + EvidenceSnippetRadius);
        var snippet = text[start..end].Trim();
        snippet = Regex.Replace(snippet, @"\s+", " ");

        if (start > 0)
        {
            snippet = $"... {snippet}";
        }

        if (end < text.Length)
        {
            snippet = $"{snippet} ...";
        }

        return snippet;
    }

    private static string TrimEvidenceSnippet(string text)
    {
        var snippet = Regex.Replace(text.Trim(), @"\s+", " ");
        return snippet.Length <= 500 ? snippet : $"{snippet[..497]}...";
    }

    private static bool ContainsAny(string text, IEnumerable<string> terms) =>
        terms.Any(term => ContainsTerm(text, term));

    private static bool ContainsTerm(string text, string term)
    {
        var pattern = $@"\b{Regex.Escape(term)}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
