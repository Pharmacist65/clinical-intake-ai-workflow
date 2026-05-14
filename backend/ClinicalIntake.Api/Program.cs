using System.Text.Json.Serialization;
using ClinicalIntake.Api.Contracts;
using ClinicalIntake.Api.Data;
using ClinicalIntake.Api.Models;
using ClinicalIntake.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=clinical-intake.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IAiSummaryService, MockAiSummaryService>();
builder.Services.AddScoped<MedicationContextService>();
builder.Services.AddScoped<IntakeWorkflowService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(exceptionHandler =>
{
    exceptionHandler.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiErrors.Unexpected());
    });
});

app.UseCors("Frontend");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/intakes", async (
    CreateIntakeRequest request,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var validation = IntakeRequestValidator.ValidateCreate(request);
    if (!validation.IsValid)
    {
        return ApiErrors.Validation(validation);
    }

    var intake = await workflow.CreateIntakeAsync(request, cancellationToken);
    return Results.Created($"/api/intakes/{intake.Id}", IntakeMapper.ToDetail(intake));
});

app.MapGet("/api/intakes", async (
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intakes = await workflow.ListIntakesAsync(cancellationToken);
    return Results.Ok(intakes.Select(IntakeMapper.ToListItem));
});

app.MapGet("/api/intakes/{id:int}", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intake = await workflow.GetIntakeAsync(id, cancellationToken);
    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
});

app.MapPost("/api/intakes/{id:int}/generate-summary", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intake = await workflow.GenerateSummaryAsync(id, "MockAiSummaryService", cancellationToken);
    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
});

app.MapGet("/api/review-queue", async (
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intakes = await workflow.GetReviewQueueAsync(cancellationToken);
    return Results.Ok(intakes.Select(IntakeMapper.ToReviewQueueItem));
});

app.MapPatch("/api/intakes/{id:int}/review-status", async (
    int id,
    UpdateReviewStatusRequest request,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var validation = IntakeRequestValidator.ValidateReviewStatus(request);
    if (!validation.IsValid)
    {
        return ApiErrors.Validation(validation);
    }

    var reviewStatus = Enum.Parse<ReviewStatus>(request.ReviewStatus, ignoreCase: true);
    var intake = await workflow.UpdateReviewStatusAsync(
        id,
        reviewStatus,
        request.Actor.Trim(),
        cancellationToken);

    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
});

app.MapGet("/api/intakes/{id:int}/audit-log", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var auditLogs = await workflow.GetAuditLogsAsync(id, cancellationToken);
    return auditLogs is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(auditLogs.Select(IntakeMapper.ToAuditLogResponse));
});

app.MapPost("/api/intakes/{id:int}/medications", async (
    int id,
    CreateMedicationEntryRequest request,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var validation = IntakeRequestValidator.ValidateMedication(request);
    if (!validation.IsValid)
    {
        return ApiErrors.Validation(validation);
    }

    var medication = await workflow.AddMedicationAsync(id, request, cancellationToken);
    return medication is null
        ? ApiErrors.NotFound("Intake")
        : Results.Created($"/api/intakes/{id}/medications/{medication.Id}", IntakeMapper.ToMedicationEntryResponse(medication));
});

app.MapGet("/api/intakes/{id:int}/medications", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var medications = await workflow.ListMedicationsAsync(id, cancellationToken);
    return medications is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(medications.Select(IntakeMapper.ToMedicationEntryResponse));
});

app.MapPost("/api/intakes/{id:int}/analyse-medication-context", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intake = await workflow.AnalyseMedicationContextAsync(id, "MedicationContextService", cancellationToken);
    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
});

app.MapGet("/api/intakes/{id:int}/medication-signals", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var signals = await workflow.GetMedicationSignalsAsync(id, cancellationToken);
    return signals is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(signals.Select(IntakeMapper.ToMedicationSignalResponse));
});

app.Run();
