using ClinicalIntake.Api.Models;

namespace ClinicalIntake.Api.Contracts;

public sealed record FhirStyleExportResponse(
    string ResourceType,
    string ExportMode,
    string Disclaimer,
    DateTime GeneratedAt,
    FhirStylePatientResource Patient,
    FhirStyleQuestionnaireResponseResource IntakeQuestionnaireResponse,
    FhirStyleTaskResource ReviewTask,
    IReadOnlyList<FhirStyleMedicationStatementResource> MedicationStatements,
    IReadOnlyList<FhirStyleProvenanceResource> Provenance,
    IReadOnlyList<FhirStyleAuditEventResource> AuditEvents);

public sealed record FhirStylePatientResource(
    string ResourceType,
    string Id,
    string DisplayName,
    int Age,
    string SafetyNote);

public sealed record FhirStyleQuestionnaireResponseResource(
    string ResourceType,
    string Id,
    string Status,
    DateTime Authored,
    string Source,
    string CreatedBy,
    IReadOnlyList<FhirStyleQuestionnaireItem> Items);

public sealed record FhirStyleQuestionnaireItem(
    string LinkId,
    string Text,
    string Answer);

public sealed record FhirStyleTaskResource(
    string ResourceType,
    string Id,
    string Status,
    string Intent,
    string Description,
    string LocalReviewStatus,
    string ForReference);

public sealed record FhirStyleMedicationStatementResource(
    string ResourceType,
    string Id,
    string Status,
    string MedicationText,
    string LocalCategory,
    string InformationSource,
    string? DosageText,
    DateTime? EffectiveStart,
    DateTime? EffectiveEnd,
    string? ReasonText,
    string? Note);

public sealed record FhirStyleProvenanceResource(
    string ResourceType,
    string Id,
    string TargetReference,
    DateTime Recorded,
    string Agent,
    string SourceType,
    string SourceLabel,
    string MappingNote);

public sealed record FhirStyleAuditEventResource(
    string ResourceType,
    string Id,
    string Action,
    DateTime Recorded,
    string Agent,
    string Detail,
    string EntityReference);

public static class FhirStyleExportMapper
{
    private const string ExportDisclaimer =
        "FHIR-style fictional export for interoperability discussion only. This is not a validated FHIR implementation, not an EHR integration, and must not be used with real patient data.";

    public static FhirStyleExportResponse ToExport(Intake intake)
    {
        var patientReference = $"Patient/fictional-patient-{intake.Id}";

        return new FhirStyleExportResponse(
            ResourceType: "Bundle",
            ExportMode: "FHIR-style fictional export",
            Disclaimer: ExportDisclaimer,
            GeneratedAt: DateTime.UtcNow,
            Patient: new FhirStylePatientResource(
                ResourceType: "Patient",
                Id: $"fictional-patient-{intake.Id}",
                DisplayName: intake.PatientAlias,
                Age: intake.Age,
                SafetyNote: "Fictional patient alias only. No real patient identifiers are exported."),
            IntakeQuestionnaireResponse: BuildQuestionnaireResponse(intake),
            ReviewTask: BuildReviewTask(intake, patientReference),
            MedicationStatements: intake.MedicationEntries
                .OrderBy(medication => medication.CreatedAt)
                .Select(BuildMedicationStatement)
                .ToList(),
            Provenance: intake.ContextEvents
                .OrderBy(contextEvent => contextEvent.CreatedAt)
                .Select(BuildProvenance)
                .ToList(),
            AuditEvents: intake.AuditLogs
                .OrderBy(log => log.Timestamp)
                .Select(log => BuildAuditEvent(log, intake.Id))
                .ToList());
    }

    private static FhirStyleQuestionnaireResponseResource BuildQuestionnaireResponse(Intake intake)
    {
        var items = new List<FhirStyleQuestionnaireItem>
        {
            new("intake-text", "Original intake text", intake.IntakeText),
            new("source", "Source", intake.Source),
            new("review-status", "Local review status", intake.ReviewStatus.ToString()),
            new("created-by", "Created by", intake.CreatedBy)
        };

        if (intake.AiSummary is not null)
        {
            items.Add(new FhirStyleQuestionnaireItem(
                "ai-summary-disclaimer",
                "Generated summary safety disclaimer",
                intake.AiSummary.Disclaimer));
        }

        return new FhirStyleQuestionnaireResponseResource(
            ResourceType: "QuestionnaireResponse",
            Id: $"intake-questionnaire-response-{intake.Id}",
            Status: "completed",
            Authored: intake.CreatedAt,
            Source: intake.Source,
            CreatedBy: intake.CreatedBy,
            Items: items);
    }

    private static FhirStyleTaskResource BuildReviewTask(Intake intake, string patientReference) =>
        new(
            ResourceType: "Task",
            Id: $"review-task-{intake.Id}",
            Status: MapTaskStatus(intake.ReviewStatus),
            Intent: "workflow",
            Description: "Human review workflow task for a fictional intake case.",
            LocalReviewStatus: intake.ReviewStatus.ToString(),
            ForReference: patientReference);

    private static FhirStyleMedicationStatementResource BuildMedicationStatement(MedicationEntry medication) =>
        new(
            ResourceType: "MedicationStatement",
            Id: $"medication-statement-{medication.Id}",
            Status: MapMedicationStatementStatus(medication.Category),
            MedicationText: medication.MedicationName,
            LocalCategory: medication.Category.ToString(),
            InformationSource: medication.Source.ToString(),
            DosageText: BuildDosageText(medication),
            EffectiveStart: medication.StartedAt,
            EffectiveEnd: medication.StoppedAt,
            ReasonText: medication.ReasonForUse,
            Note: medication.Notes);

    private static FhirStyleProvenanceResource BuildProvenance(ContextEvent contextEvent) =>
        new(
            ResourceType: "Provenance",
            Id: $"context-provenance-{contextEvent.Id}",
            TargetReference: $"ContextEvent/{contextEvent.Id}",
            Recorded: contextEvent.CreatedAt,
            Agent: contextEvent.CreatedBy,
            SourceType: contextEvent.SourceType.ToString(),
            SourceLabel: contextEvent.SourceLabel,
            MappingNote: "Internal source provenance mapped to a FHIR-style example resource for discussion only.");

    private static FhirStyleAuditEventResource BuildAuditEvent(AuditLog log, int intakeId) =>
        new(
            ResourceType: "AuditEvent",
            Id: $"audit-event-{log.Id}",
            Action: log.Action,
            Recorded: log.Timestamp,
            Agent: log.Actor,
            Detail: log.Details,
            EntityReference: $"Intake/{intakeId}");

    private static string MapTaskStatus(ReviewStatus reviewStatus) =>
        reviewStatus switch
        {
            ReviewStatus.Reviewed => "completed",
            ReviewStatus.NeedsReview => "requested",
            _ => "draft"
        };

    private static string MapMedicationStatementStatus(MedicationCategory category) =>
        category switch
        {
            MedicationCategory.Current or MedicationCategory.OTC => "active",
            MedicationCategory.Recent => "intended",
            _ => "completed"
        };

    private static string? BuildDosageText(MedicationEntry medication)
    {
        var parts = new[] { medication.Dose, medication.Frequency, medication.Route }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }
}
