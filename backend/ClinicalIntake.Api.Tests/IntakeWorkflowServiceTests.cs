using ClinicalIntake.Api.Contracts;
using ClinicalIntake.Api.Data;
using ClinicalIntake.Api.Models;
using ClinicalIntake.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicalIntake.Api.Tests;

public sealed class IntakeWorkflowServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly IntakeWorkflowService _workflow;

    public IntakeWorkflowServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _workflow = new IntakeWorkflowService(_db, new MockAiSummaryService(), new MedicationContextService());
    }

    [Fact]
    public async Task CreateIntakeAsync_PersistsNewIntakeAndAuditLog()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());

        Assert.True(intake.Id > 0);
        Assert.Equal(ReviewStatus.New, intake.ReviewStatus);
        Assert.Single(await _db.Intakes.ToListAsync());
        Assert.Contains(intake.AuditLogs, log => log.Action == "IntakeCreated");
    }

    [Fact]
    public void ValidateCreate_WithMissingRequiredFields_ReturnsValidationErrors()
    {
        var validation = IntakeRequestValidator.ValidateCreate(new CreateIntakeRequest("", -1, "", "", ""));

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Field == nameof(CreateIntakeRequest.PatientAlias));
        Assert.Contains(validation.Errors, error => error.Field == nameof(CreateIntakeRequest.Age));
        Assert.Contains(validation.Errors, error => error.Field == nameof(CreateIntakeRequest.IntakeText));
        Assert.Contains(validation.Errors, error => error.Field == nameof(CreateIntakeRequest.Source));
        Assert.Contains(validation.Errors, error => error.Field == nameof(CreateIntakeRequest.CreatedBy));
    }

    [Fact]
    public void ValidateReviewStatus_WithUnknownStatus_ReturnsValidationError()
    {
        var validation = IntakeRequestValidator.ValidateReviewStatus(
            new UpdateReviewStatusRequest("Closed", "clinical-reviewer"));

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Field == nameof(UpdateReviewStatusRequest.ReviewStatus));
    }

    [Fact]
    public async Task GenerateSummaryAsync_CreatesStructuredSummary()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Parent reports school concerns, poor sleep, reduced attention and communication changes over several months."));

        var updated = await _workflow.GenerateSummaryAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.NotNull(updated.AiSummary);
        Assert.Contains("School", updated.AiSummary.PresentingConcerns);
        Assert.Contains("Sleep", updated.AiSummary.PresentingConcerns);
        Assert.True(updated.AiSummary.ConfidenceScore > 0);
        Assert.Equal(AiSafety.Disclaimer, updated.AiSummary.Disclaimer);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithHighRiskText_SetsReviewStatusToNeedsReview()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Family reports self-harm comments and suicidal language alongside school withdrawal and sleep disruption."));

        var updated = await _workflow.GenerateSummaryAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.Equal(ReviewStatus.NeedsReview, updated.ReviewStatus);
        Assert.Contains(updated.RiskFlags, flag => flag.Severity == RiskSeverity.High);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithLowConfidenceText_SetsReviewStatusToNeedsReview()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest("Brief unclear note."));

        var updated = await _workflow.GenerateSummaryAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.NotNull(updated.AiSummary);
        Assert.Equal(ReviewStatus.NeedsReview, updated.ReviewStatus);
        Assert.True(updated.AiSummary.ConfidenceScore < 0.75m);
    }

    [Fact]
    public async Task GenerateSummaryAsync_CreatesAuditLog()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());

        var updated = await _workflow.GenerateSummaryAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.Contains(updated.AuditLogs, log => log.Action == "AiSummaryGenerated");
    }

    [Fact]
    public async Task GetReviewQueueAsync_ReturnsOnlyNeedsReviewIntakes()
    {
        var routine = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Parent reports school concerns, poor sleep, reduced attention and communication changes over several months."));
        await _workflow.GenerateSummaryAsync(routine.Id, "test");

        var highRisk = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Family reports safeguarding concerns and self-harm language alongside sleep disruption."));
        await _workflow.GenerateSummaryAsync(highRisk.Id, "test");

        var queue = await _workflow.GetReviewQueueAsync();

        Assert.DoesNotContain(queue, intake => intake.Id == routine.Id);
        Assert.Contains(queue, intake => intake.Id == highRisk.Id);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WhenRepeated_ReplacesPreviousRiskFlagsAndUpdatesExistingSummary()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Family reports safeguarding concerns, urgent change and sleep disruption."));
        var firstSummary = await _workflow.GenerateSummaryAsync(intake.Id, "test");

        Assert.NotNull(firstSummary);
        var summaryId = firstSummary.AiSummary?.Id;
        Assert.Contains(firstSummary.RiskFlags, flag => flag.Severity == RiskSeverity.High);

        var tracked = await _db.Intakes.FirstAsync(existing => existing.Id == intake.Id);
        tracked.IntakeText = "Parent reports school concerns, poor sleep, reduced attention and communication changes over several months.";
        await _db.SaveChangesAsync();

        var secondSummary = await _workflow.GenerateSummaryAsync(intake.Id, "test");

        Assert.NotNull(secondSummary);
        Assert.Equal(summaryId, secondSummary.AiSummary?.Id);
        Assert.DoesNotContain(secondSummary.RiskFlags, flag => flag.Severity == RiskSeverity.High);
        Assert.Equal(ReviewStatus.New, secondSummary.ReviewStatus);
    }

    [Fact]
    public async Task UpdateReviewStatusAsync_CanMarkReviewed()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Family reports safeguarding worries and urgent changes in sleep, school engagement and communication."));
        await _workflow.GenerateSummaryAsync(intake.Id, "test");

        var updated = await _workflow.UpdateReviewStatusAsync(intake.Id, ReviewStatus.Reviewed, "clinical-reviewer");

        Assert.NotNull(updated);
        Assert.Equal(ReviewStatus.Reviewed, updated.ReviewStatus);
        Assert.Contains(updated.AuditLogs, log =>
            log.Action == "ReviewStatusUpdated" && log.Actor == "clinical-reviewer");
    }

    [Fact]
    public async Task AddMedicationAsync_PersistsMedicationEntry()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());

        var medication = await _workflow.AddMedicationAsync(
            intake.Id,
            MedicationRequest("Paracetamol", category: "Current", dose: "500 mg", frequency: "as needed"));

        Assert.NotNull(medication);
        Assert.True(medication.Id > 0);
        Assert.Equal("Paracetamol", medication.MedicationName);
        Assert.Equal("paracetamol", medication.NormalizedName);
    }

    [Fact]
    public async Task ListMedicationsAsync_ReturnsMedicationEntries()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());
        await _workflow.AddMedicationAsync(intake.Id, MedicationRequest("Paracetamol"));
        await _workflow.AddMedicationAsync(intake.Id, MedicationRequest("Ibuprofen", category: "OTC"));

        var medications = await _workflow.ListMedicationsAsync(intake.Id);

        Assert.NotNull(medications);
        Assert.Equal(2, medications.Count);
    }

    [Fact]
    public async Task AnalyseMedicationContext_WithNsaidMention_CreatesMedicationSignal()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());
        await _workflow.AddMedicationAsync(
            intake.Id,
            MedicationRequest("Ibuprofen", category: "OTC", dose: "200 mg", frequency: "three times daily"));

        var updated = await _workflow.AnalyseMedicationContextAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.Contains(updated.MedicationSignals, signal =>
            signal.Label == "OTC NSAID context" && signal.Severity == RiskSeverity.Medium);
    }

    [Fact]
    public async Task AnalyseMedicationContext_WithNsaidAndSafetyContext_CreatesHighSeveritySignal()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Family reports asthma history and recent OTC pain relief use."));
        await _workflow.AddMedicationAsync(
            intake.Id,
            MedicationRequest("Nurofen", category: "OTC", dose: "200 mg", frequency: "twice daily"));

        var updated = await _workflow.AnalyseMedicationContextAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.Contains(updated.MedicationSignals, signal =>
            signal.Label == "Medication safety review signal" && signal.Severity == RiskSeverity.High);
    }

    [Fact]
    public async Task AnalyseMedicationContext_WithNsaidMentionInNotes_CreatesHighSeveritySignal()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());
        var medication = await _workflow.AddMedicationAsync(
            intake.Id,
            MedicationRequest(
                "Pain relief tablet",
                category: "OTC",
                dose: "unknown",
                frequency: "unknown",
                notes: "Family reports NSAID use and asthma history."));

        var updated = await _workflow.AnalyseMedicationContextAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.NotNull(medication);
        Assert.Contains(updated.MedicationSignals, signal =>
            signal.MedicationEntryId == medication.Id
            && signal.Label == "Medication safety review signal"
            && signal.Severity == RiskSeverity.High);
    }

    [Fact]
    public async Task AnalyseMedicationContext_WithIncompleteCurrentMedication_CreatesIncompleteHistorySignal()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());
        await _workflow.AddMedicationAsync(intake.Id, MedicationRequest("Cetirizine", category: "Current"));

        var updated = await _workflow.AnalyseMedicationContextAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.Contains(updated.MedicationSignals, signal =>
            signal.Label == "Incomplete medication history" && signal.Severity == RiskSeverity.Low);
    }

    [Fact]
    public async Task AnalyseMedicationContext_CreatesAuditLog()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest());
        await _workflow.AddMedicationAsync(intake.Id, MedicationRequest("Ibuprofen", category: "OTC"));

        var updated = await _workflow.AnalyseMedicationContextAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.Contains(updated.AuditLogs, log => log.Action == "MedicationContextAnalysed");
    }

    [Fact]
    public async Task AnalyseMedicationContext_WithHighSeveritySignal_RoutesIntakeToNeedsReview()
    {
        var intake = await _workflow.CreateIntakeAsync(DefaultRequest(
            "Family reports kidney history and recent OTC medicine use."));
        await _workflow.AddMedicationAsync(
            intake.Id,
            MedicationRequest("Naproxen", category: "OTC", dose: "250 mg", frequency: "twice daily"));

        var updated = await _workflow.AnalyseMedicationContextAsync(intake.Id, "test");

        Assert.NotNull(updated);
        Assert.Equal(ReviewStatus.NeedsReview, updated.ReviewStatus);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static CreateIntakeRequest DefaultRequest(string? intakeText = null) =>
        new(
            "Patient A",
            12,
            intakeText ?? "Parent reports sleep problems, school difficulties and attention concerns over the last term.",
            "family phone note",
            "demo-user");

    private static CreateMedicationEntryRequest MedicationRequest(
        string medicationName,
        string category = "Current",
        string? dose = null,
        string? frequency = null,
        string? notes = null) =>
        new(
            medicationName,
            category,
            dose,
            null,
            frequency,
            null,
            null,
            null,
            "FamilyReported",
            null,
            notes);
}
