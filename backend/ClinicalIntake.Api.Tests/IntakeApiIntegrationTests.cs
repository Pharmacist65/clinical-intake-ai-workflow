using System.Net;
using System.Net.Http.Json;
using ClinicalIntake.Api.Contracts;
using ClinicalIntake.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ClinicalIntake.Api.Tests;

public sealed class IntakeApiIntegrationTests : IClassFixture<ClinicalIntakeApiFactory>
{
    private readonly HttpClient _client;

    public IntakeApiIntegrationTests(ClinicalIntakeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
    }

    [Fact]
    public async Task CreateAndGetIntake_ReturnsPersistedIntake()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/intakes", new CreateIntakeRequest(
            "API Patient A",
            11,
            "Parent reports sleep concerns, school support needs and attention changes.",
            "api integration test",
            "api-test"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();
        Assert.NotNull(created);
        Assert.Equal("API Patient A", created.PatientAlias);

        var getResponse = await _client.GetAsync($"/api/intakes/{created.Id}");

        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task UpdateReviewStatus_WithReviewNote_AddsAuditLogDetail()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/intakes", new CreateIntakeRequest(
            "API Patient B",
            15,
            "Family reports urgent sleep disruption and safeguarding language requiring human review.",
            "api integration test",
            "api-test"));
        var created = await createResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();

        Assert.NotNull(created);

        var reviewResponse = await _client.PatchAsJsonAsync(
            $"/api/intakes/{created.Id}/review-status",
            new UpdateReviewStatusRequest(
                "Reviewed",
                "api-reviewer",
                "Reviewed by qualified human reviewer in demo workflow."));

        reviewResponse.EnsureSuccessStatusCode();
        var reviewed = await reviewResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();

        Assert.NotNull(reviewed);
        Assert.Equal("Reviewed", reviewed.ReviewStatus);
        Assert.Contains(reviewed.AuditLogs, log =>
            log.Action == "ReviewStatusUpdated"
            && log.Actor == "api-reviewer"
            && log.Details.Contains("Reviewed by qualified human reviewer", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SwaggerJson_ListsCoreIntakeEndpoint()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/intakes", body, StringComparison.Ordinal);
        Assert.Contains("/api/intakes/{id}/context-events", body, StringComparison.Ordinal);
        Assert.Contains("/api/intakes/{id}/transcript-context", body, StringComparison.Ordinal);
        Assert.Contains("/api/intakes/{id}/document-context", body, StringComparison.Ordinal);
        Assert.Contains("/api/intakes/{id}/medication-documentation-quality", body, StringComparison.Ordinal);
        Assert.Contains("Clinical Intake AI Workflow API", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContextEventEndpoints_CreateAndListContextEvents()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/intakes", new CreateIntakeRequest(
            "API Patient Context",
            13,
            "Family reports school concerns and a follow-up note from a fictional transcript.",
            "api integration test",
            "api-test"));
        var created = await createResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();

        Assert.NotNull(created);

        var contextResponse = await _client.PostAsJsonAsync(
            $"/api/intakes/{created.Id}/context-events",
            new CreateContextEventRequest(
                "TranscriptText",
                "Fictional family call transcript",
                "Family described sleep disruption and school support needs.",
                null,
                "api-test",
                0.88m,
                null));

        Assert.Equal(HttpStatusCode.Created, contextResponse.StatusCode);
        var contextEvent = await contextResponse.Content.ReadFromJsonAsync<ContextEventResponse>();
        Assert.NotNull(contextEvent);
        Assert.Equal("TranscriptText", contextEvent.SourceType);

        var listResponse = await _client.GetAsync($"/api/intakes/{created.Id}/context-events");

        listResponse.EnsureSuccessStatusCode();
        var contextEvents = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<ContextEventResponse>>();
        Assert.NotNull(contextEvents);
        Assert.Contains(contextEvents, item => item.Id == contextEvent.Id);
    }

    [Fact]
    public async Task TranscriptContextEndpoint_CreatesTranscriptContextEvent()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/intakes", new CreateIntakeRequest(
            "API Patient Transcript",
            12,
            "Initial note says family will provide further context.",
            "api integration test",
            "api-test"));
        var created = await createResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();

        Assert.NotNull(created);

        var transcriptResponse = await _client.PostAsJsonAsync(
            $"/api/intakes/{created.Id}/transcript-context",
            new CreateTranscriptContextRequest(
                "Mock family call transcript",
                "Family describes school support needs and sleep disruption in fictional transcript text.",
                null,
                "api-test",
                0.91m,
                "Fictional family call"));

        Assert.Equal(HttpStatusCode.Created, transcriptResponse.StatusCode);
        var contextEvent = await transcriptResponse.Content.ReadFromJsonAsync<ContextEventResponse>();

        Assert.NotNull(contextEvent);
        Assert.Equal("TranscriptText", contextEvent.SourceType);
        Assert.Equal("Mock family call transcript", contextEvent.SourceLabel);
        Assert.NotNull(contextEvent.MetadataJson);
        Assert.Contains("mock-transcript", contextEvent.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentContextEndpoint_CreatesDocumentContextEvent()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/intakes", new CreateIntakeRequest(
            "API Patient Document",
            10,
            "Initial note says a fictional referral document is available.",
            "api integration test",
            "api-test"));
        var created = await createResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();

        Assert.NotNull(created);

        var documentResponse = await _client.PostAsJsonAsync(
            $"/api/intakes/{created.Id}/document-context",
            new CreateDocumentContextRequest(
                "Mock referral note",
                "Fictional referral note describes school support needs and urgent sleep disruption.",
                null,
                "api-test",
                0.87m,
                "Referral note",
                "page 1"));

        Assert.Equal(HttpStatusCode.Created, documentResponse.StatusCode);
        var contextEvent = await documentResponse.Content.ReadFromJsonAsync<ContextEventResponse>();

        Assert.NotNull(contextEvent);
        Assert.Equal("DocumentText", contextEvent.SourceType);
        Assert.Equal("Mock referral note", contextEvent.SourceLabel);
        Assert.NotNull(contextEvent.MetadataJson);
        Assert.Contains("mock-document-ocr", contextEvent.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MedicationDocumentationQualityEndpoint_ReturnsDocumentationOnlyAssessment()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/intakes", new CreateIntakeRequest(
            "API Patient C",
            14,
            "Family reports sleep concerns and an unclear current medication history.",
            "api integration test",
            "api-test"));
        var created = await createResponse.Content.ReadFromJsonAsync<IntakeDetailResponse>();

        Assert.NotNull(created);

        await _client.PostAsJsonAsync($"/api/intakes/{created.Id}/medications", new CreateMedicationEntryRequest(
            "Cetirizine",
            "Current",
            null,
            null,
            null,
            null,
            null,
            null,
            "Unknown",
            null,
            null));

        var response = await _client.GetAsync($"/api/intakes/{created.Id}/medication-documentation-quality");

        response.EnsureSuccessStatusCode();
        var quality = await response.Content.ReadFromJsonAsync<MedicationDocumentationQualityResponse>();
        Assert.NotNull(quality);
        Assert.Equal("Incomplete", quality.Status);
        Assert.Contains(quality.Issues, issue => issue.Field == "source");
        Assert.Contains("not a clinical risk score", quality.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ClinicalIntakeApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiDocs:Enabled"] = "true",
                ["DemoData:SeedOnStartup"] = "false",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
