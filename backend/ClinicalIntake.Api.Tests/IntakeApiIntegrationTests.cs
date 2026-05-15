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
        Assert.Contains("Clinical Intake AI Workflow API", body, StringComparison.Ordinal);
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
