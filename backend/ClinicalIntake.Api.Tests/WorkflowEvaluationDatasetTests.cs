using System.Text.Json;
using ClinicalIntake.Api.Contracts;
using ClinicalIntake.Api.Data;
using ClinicalIntake.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicalIntake.Api.Tests;

public sealed class WorkflowEvaluationDatasetTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IEnumerable<object[]> EvaluationCases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "evaluation-cases.json");
        var json = File.ReadAllText(path);
        var cases = JsonSerializer.Deserialize<IReadOnlyList<EvaluationCase>>(json, JsonOptions)
            ?? throw new InvalidOperationException("Evaluation dataset could not be loaded.");

        return cases.Select(evaluationCase => new object[] { evaluationCase });
    }

    [Theory]
    [MemberData(nameof(EvaluationCases))]
    public async Task FictionalEvaluationCases_MatchExpectedWorkflowSignals(EvaluationCase evaluationCase)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var workflow = new IntakeWorkflowService(db, new MockAiSummaryService(), new MedicationContextService());
        var intake = await workflow.CreateIntakeAsync(new CreateIntakeRequest(
            $"Evaluation {evaluationCase.Id}",
            evaluationCase.Age,
            evaluationCase.IntakeText,
            "fictional evaluation dataset",
            "evaluation-test"));

        var afterSummary = await workflow.GenerateSummaryAsync(intake.Id, "evaluation-test");
        Assert.NotNull(afterSummary);
        Assert.NotNull(afterSummary.AiSummary);

        foreach (var medication in evaluationCase.Medications)
        {
            await workflow.AddMedicationAsync(intake.Id, new CreateMedicationEntryRequest(
                medication.MedicationName,
                medication.Category,
                medication.Dose,
                medication.Route,
                medication.Frequency,
                null,
                null,
                medication.ReasonForUse,
                medication.Source,
                medication.PrescribedBy,
                medication.Notes));
        }

        var final = await workflow.AnalyseMedicationContextAsync(intake.Id, "evaluation-test");
        var medicationDocumentationQuality = await workflow.GetMedicationDocumentationQualityAsync(intake.Id);

        Assert.NotNull(final);
        Assert.NotNull(medicationDocumentationQuality);
        Assert.Equal(evaluationCase.Expected.ReviewStatus, final.ReviewStatus.ToString());
        Assert.Equal(evaluationCase.Expected.MedicationDocumentationStatus, medicationDocumentationQuality.Status);

        if (evaluationCase.Expected.MinimumConfidenceScore is not null)
        {
            Assert.True(
                afterSummary.AiSummary.ConfidenceScore >= evaluationCase.Expected.MinimumConfidenceScore.Value,
                $"{evaluationCase.Id} expected confidence >= {evaluationCase.Expected.MinimumConfidenceScore}.");
        }

        if (evaluationCase.Expected.MaximumConfidenceScore is not null)
        {
            Assert.True(
                afterSummary.AiSummary.ConfidenceScore <= evaluationCase.Expected.MaximumConfidenceScore.Value,
                $"{evaluationCase.Id} expected confidence <= {evaluationCase.Expected.MaximumConfidenceScore}.");
        }

        AssertExpectedLabels(
            evaluationCase.Id,
            evaluationCase.Expected.RiskLabels,
            final.RiskFlags.Select(flag => flag.Label));

        AssertExpectedLabels(
            evaluationCase.Id,
            evaluationCase.Expected.MedicationSignalLabels,
            final.MedicationSignals.Select(signal => signal.Label));
    }

    private static void AssertExpectedLabels(
        string caseId,
        IReadOnlyCollection<string> expected,
        IEnumerable<string> actual)
    {
        var actualSet = actual.Order().ToList();
        var expectedSet = expected.Order().ToList();

        Assert.True(
            actualSet.SequenceEqual(expectedSet),
            $"{caseId} expected labels [{string.Join(", ", expectedSet)}] but received [{string.Join(", ", actualSet)}].");
    }
}

public sealed record EvaluationCase(
    string Id,
    string Description,
    int Age,
    string IntakeText,
    IReadOnlyList<EvaluationMedication> Medications,
    EvaluationExpected Expected)
{
    public override string ToString() => Id;
}

public sealed record EvaluationMedication(
    string MedicationName,
    string Category,
    string? Dose,
    string? Route,
    string? Frequency,
    string? ReasonForUse,
    string Source,
    string? PrescribedBy,
    string? Notes);

public sealed record EvaluationExpected(
    string ReviewStatus,
    decimal? MinimumConfidenceScore,
    decimal? MaximumConfidenceScore,
    IReadOnlyList<string> RiskLabels,
    IReadOnlyList<string> MedicationSignalLabels,
    string MedicationDocumentationStatus);
