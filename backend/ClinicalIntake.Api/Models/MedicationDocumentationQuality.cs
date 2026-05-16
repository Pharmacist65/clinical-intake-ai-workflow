namespace ClinicalIntake.Api.Models;

public sealed record MedicationDocumentationQuality(
    int? Score,
    string Status,
    string Summary,
    IReadOnlyList<MedicationDocumentationIssue> Issues,
    string Disclaimer);

public sealed record MedicationDocumentationIssue(
    int? MedicationEntryId,
    string MedicationName,
    string Field,
    string Reason);
