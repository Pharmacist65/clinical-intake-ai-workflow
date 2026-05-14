using ClinicalIntake.Api.Models;

namespace ClinicalIntake.Api.Contracts;

public static class IntakeMapper
{
    public static IntakeListItemResponse ToListItem(Intake intake) =>
        new(
            intake.Id,
            intake.PatientAlias,
            intake.Age,
            intake.Source,
            intake.ReviewStatus.ToString(),
            intake.CreatedAt,
            intake.CreatedBy,
            HighestSeverity(intake)?.ToString());

    public static IntakeDetailResponse ToDetail(Intake intake) =>
        new(
            intake.Id,
            intake.PatientAlias,
            intake.Age,
            intake.IntakeText,
            intake.Source,
            intake.ReviewStatus.ToString(),
            intake.CreatedAt,
            intake.CreatedBy,
            intake.AiSummary is null ? null : ToAiSummaryResponse(intake.AiSummary),
            intake.RiskFlags
                .OrderByDescending(flag => flag.Severity)
                .ThenBy(flag => flag.Label)
                .Select(ToRiskFlagResponse)
                .ToList(),
            intake.MedicationEntries
                .OrderByDescending(medication => medication.StartedAt ?? medication.CreatedAt)
                .ThenBy(medication => medication.MedicationName)
                .Select(ToMedicationEntryResponse)
                .ToList(),
            intake.MedicationSignals
                .OrderByDescending(signal => signal.Severity)
                .ThenBy(signal => signal.Label)
                .Select(ToMedicationSignalResponse)
                .ToList(),
            intake.AuditLogs
                .OrderBy(log => log.Timestamp)
                .Select(ToAuditLogResponse)
                .ToList());

    public static ReviewQueueItemResponse ToReviewQueueItem(Intake intake) =>
        new(
            intake.Id,
            intake.PatientAlias,
            intake.Age,
            intake.Source,
            intake.CreatedAt,
            (HighestSeverity(intake) ?? RiskSeverity.Low).ToString(),
            intake.RiskFlags
                .OrderByDescending(flag => flag.Severity)
                .ThenBy(flag => flag.Label)
                .Select(ToRiskFlagResponse)
                .ToList());

    public static AuditLogResponse ToAuditLogResponse(AuditLog log) =>
        new(log.Id, log.IntakeId, log.Action, log.Actor, log.Timestamp, log.Details);

    public static MedicationEntryResponse ToMedicationEntryResponse(MedicationEntry medication) =>
        new(
            medication.Id,
            medication.IntakeId,
            medication.MedicationName,
            medication.NormalizedName,
            medication.Category.ToString(),
            medication.Dose,
            medication.Route,
            medication.Frequency,
            medication.StartedAt,
            medication.StoppedAt,
            medication.ReasonForUse,
            medication.Source.ToString(),
            medication.PrescribedBy,
            medication.Notes,
            medication.CreatedAt);

    public static MedicationSignalResponse ToMedicationSignalResponse(MedicationSignal signal) =>
        new(
            signal.Id,
            signal.IntakeId,
            signal.MedicationEntryId,
            signal.Label,
            signal.Severity.ToString(),
            signal.Rationale,
            signal.ReviewerQuestion,
            signal.CreatedAt);

    private static AiSummaryResponse ToAiSummaryResponse(AiSummary summary) =>
        new(
            summary.Id,
            summary.IntakeId,
            summary.PresentingConcerns,
            summary.RelevantHistory,
            summary.PossibleRisks,
            summary.RecommendedNextStep,
            summary.ConfidenceScore,
            summary.GeneratedAt,
            summary.Disclaimer);

    private static RiskFlagResponse ToRiskFlagResponse(RiskFlag flag) =>
        new(flag.Id, flag.IntakeId, flag.Label, flag.Severity.ToString(), flag.Reason);

    private static RiskSeverity? HighestSeverity(Intake intake)
    {
        var severities = intake.RiskFlags
            .Select(flag => flag.Severity)
            .Concat(intake.MedicationSignals.Select(signal => signal.Severity))
            .ToList();

        if (severities.Count == 0)
        {
            return null;
        }

        return severities.Max();
    }
}
