using ClinicalIntake.Api.Contracts;
using ClinicalIntake.Api.Data;
using ClinicalIntake.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicalIntake.Api.Services;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        if (await db.Intakes.AnyAsync(cancellationToken))
        {
            return;
        }

        var workflow = serviceProvider.GetRequiredService<IntakeWorkflowService>();

        var pharmacyReview = await workflow.CreateIntakeAsync(new CreateIntakeRequest(
            "Demo Patient A",
            12,
            "Family reports school difficulties, poor sleep, reduced attention and recent meltdowns. Family also mentions intermittent Nurofen use and asthma history, but dose and duration are unclear.",
            "family phone note",
            "demo-user"), cancellationToken);
        await workflow.GenerateSummaryAsync(pharmacyReview.Id, "DemoDataSeeder", cancellationToken);
        await workflow.AddMedicationAsync(pharmacyReview.Id, new CreateMedicationEntryRequest(
            "Nurofen",
            "OTC",
            null,
            "oral",
            null,
            null,
            null,
            "Pain relief reported by family",
            "FamilyReported",
            null,
            "Asthma history mentioned. Dose and duration unclear."), cancellationToken);
        await workflow.AnalyseMedicationContextAsync(pharmacyReview.Id, "DemoDataSeeder", cancellationToken);

        var documentationQuality = await workflow.CreateIntakeAsync(new CreateIntakeRequest(
            "Demo Patient B",
            72,
            "Patient reports sleep disruption, recent falls concern and several current medicines. The intake note is mainly useful for documenting medication-history completeness before clinician review.",
            "clinic intake form",
            "demo-user"), cancellationToken);
        await workflow.GenerateSummaryAsync(documentationQuality.Id, "DemoDataSeeder", cancellationToken);
        await AddCurrentMedicationAsync(workflow, documentationQuality.Id, "Amlodipine", "5 mg", "once daily", cancellationToken);
        await AddCurrentMedicationAsync(workflow, documentationQuality.Id, "Atorvastatin", "20 mg", "once daily", cancellationToken);
        await AddCurrentMedicationAsync(workflow, documentationQuality.Id, "Lansoprazole", "15 mg", "once daily", cancellationToken);
        await AddCurrentMedicationAsync(workflow, documentationQuality.Id, "Metformin", "500 mg", "twice daily", cancellationToken);
        await AddCurrentMedicationAsync(workflow, documentationQuality.Id, "Vitamin D", null, null, cancellationToken);
        await workflow.AnalyseMedicationContextAsync(documentationQuality.Id, "DemoDataSeeder", cancellationToken);

        var reviewed = await workflow.CreateIntakeAsync(new CreateIntakeRequest(
            "Demo Patient C",
            9,
            "Parent reports communication concerns and school support questions. No urgent language is documented in this fictional demo note.",
            "school referral note",
            "demo-user"), cancellationToken);
        await workflow.GenerateSummaryAsync(reviewed.Id, "DemoDataSeeder", cancellationToken);
        await workflow.UpdateReviewStatusAsync(
            reviewed.Id,
            ReviewStatus.Reviewed,
            "demo-clinician",
            "Demo case reviewed for workflow demonstration; no clinical advice is recorded.",
            cancellationToken);

        logger.LogInformation("Seeded fictional demo intake data.");
    }

    private static Task<MedicationEntry?> AddCurrentMedicationAsync(
        IntakeWorkflowService workflow,
        int intakeId,
        string medicationName,
        string? dose,
        string? frequency,
        CancellationToken cancellationToken) =>
        workflow.AddMedicationAsync(intakeId, new CreateMedicationEntryRequest(
            medicationName,
            "Current",
            dose,
            "oral",
            frequency,
            null,
            null,
            null,
            "PatientReported",
            null,
            dose is null || frequency is null ? "Dose or frequency needs clarification." : null), cancellationToken);
}
