using System.Text.Json.Serialization;
using ClinicalIntake.Api.Contracts;
using ClinicalIntake.Api.Data;
using ClinicalIntake.Api.Models;
using ClinicalIntake.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinical Intake AI Workflow API",
        Version = "v1",
        Description = "Workflow support API for fictional clinical intake notes, mock AI summaries, medication context, human review and audit logs."
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=clinical-intake.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddAiSummaryProvider(builder.Configuration);
builder.Services.AddScoped<MedicationContextService>();
builder.Services.AddScoped<IntakeWorkflowService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
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

if (app.Configuration.GetValue<bool>("ApiDocs:Enabled") || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Clinical Intake AI Workflow API";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Clinical Intake AI Workflow API v1");
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (app.Configuration.GetValue<bool>("DemoData:SeedOnStartup"))
    {
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider, app.Logger);
    }
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck")
    .WithTags("System");

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
})
    .WithName("CreateIntake")
    .WithTags("Intakes");

app.MapGet("/api/intakes", async (
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intakes = await workflow.ListIntakesAsync(cancellationToken);
    return Results.Ok(intakes.Select(IntakeMapper.ToListItem));
})
    .WithName("ListIntakes")
    .WithTags("Intakes");

app.MapGet("/api/intakes/{id:int}", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intake = await workflow.GetIntakeAsync(id, cancellationToken);
    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
})
    .WithName("GetIntake")
    .WithTags("Intakes");

app.MapPost("/api/intakes/{id:int}/generate-summary", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intake = await workflow.GenerateSummaryAsync(id, "AiSummaryProvider", cancellationToken);
    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
})
    .WithName("GenerateSummary")
    .WithTags("Intakes");

app.MapGet("/api/review-queue", async (
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intakes = await workflow.GetReviewQueueAsync(cancellationToken);
    return Results.Ok(intakes.Select(IntakeMapper.ToReviewQueueItem));
})
    .WithName("GetReviewQueue")
    .WithTags("Review");

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
        request.ReviewNote,
        cancellationToken);

    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
})
    .WithName("UpdateReviewStatus")
    .WithTags("Review");

app.MapGet("/api/intakes/{id:int}/audit-log", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var auditLogs = await workflow.GetAuditLogsAsync(id, cancellationToken);
    return auditLogs is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(auditLogs.Select(IntakeMapper.ToAuditLogResponse));
})
    .WithName("GetAuditLog")
    .WithTags("Audit");

app.MapPost("/api/intakes/{id:int}/context-events", async (
    int id,
    CreateContextEventRequest request,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var validation = IntakeRequestValidator.ValidateContextEvent(request);
    if (!validation.IsValid)
    {
        return ApiErrors.Validation(validation);
    }

    var contextEvent = await workflow.AddContextEventAsync(id, request, cancellationToken);
    return contextEvent is null
        ? ApiErrors.NotFound("Intake")
        : Results.Created($"/api/intakes/{id}/context-events/{contextEvent.Id}", IntakeMapper.ToContextEventResponse(contextEvent));
})
    .WithName("AddContextEvent")
    .WithTags("Context Events");

app.MapGet("/api/intakes/{id:int}/context-events", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var contextEvents = await workflow.ListContextEventsAsync(id, cancellationToken);
    return contextEvents is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(contextEvents.Select(IntakeMapper.ToContextEventResponse));
})
    .WithName("ListContextEvents")
    .WithTags("Context Events");

app.MapPost("/api/intakes/{id:int}/transcript-context", async (
    int id,
    CreateTranscriptContextRequest request,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var validation = IntakeRequestValidator.ValidateTranscriptContext(request);
    if (!validation.IsValid)
    {
        return ApiErrors.Validation(validation);
    }

    var contextEvent = await workflow.AddTranscriptContextAsync(id, request, cancellationToken);
    return contextEvent is null
        ? ApiErrors.NotFound("Intake")
        : Results.Created($"/api/intakes/{id}/context-events/{contextEvent.Id}", IntakeMapper.ToContextEventResponse(contextEvent));
})
    .WithName("AddTranscriptContext")
    .WithTags("Context Events");

app.MapPost("/api/intakes/{id:int}/document-context", async (
    int id,
    CreateDocumentContextRequest request,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var validation = IntakeRequestValidator.ValidateDocumentContext(request);
    if (!validation.IsValid)
    {
        return ApiErrors.Validation(validation);
    }

    var contextEvent = await workflow.AddDocumentContextAsync(id, request, cancellationToken);
    return contextEvent is null
        ? ApiErrors.NotFound("Intake")
        : Results.Created($"/api/intakes/{id}/context-events/{contextEvent.Id}", IntakeMapper.ToContextEventResponse(contextEvent));
})
    .WithName("AddDocumentContext")
    .WithTags("Context Events");

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
})
    .WithName("AddMedication")
    .WithTags("Medication Context");

app.MapGet("/api/intakes/{id:int}/medications", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var medications = await workflow.ListMedicationsAsync(id, cancellationToken);
    return medications is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(medications.Select(IntakeMapper.ToMedicationEntryResponse));
})
    .WithName("ListMedications")
    .WithTags("Medication Context");

app.MapPost("/api/intakes/{id:int}/analyse-medication-context", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intake = await workflow.AnalyseMedicationContextAsync(id, "MedicationContextService", cancellationToken);
    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToDetail(intake));
})
    .WithName("AnalyseMedicationContext")
    .WithTags("Medication Context");

app.MapGet("/api/intakes/{id:int}/medication-signals", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var signals = await workflow.GetMedicationSignalsAsync(id, cancellationToken);
    return signals is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(signals.Select(IntakeMapper.ToMedicationSignalResponse));
})
    .WithName("ListMedicationSignals")
    .WithTags("Medication Context");

app.MapGet("/api/intakes/{id:int}/medication-documentation-quality", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var quality = await workflow.GetMedicationDocumentationQualityAsync(id, cancellationToken);
    return quality is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(IntakeMapper.ToMedicationDocumentationQualityResponse(quality));
})
    .WithName("GetMedicationDocumentationQuality")
    .WithTags("Medication Context");

app.MapGet("/api/intakes/{id:int}/fhir-style-export", async (
    int id,
    IntakeWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var intake = await workflow.GetIntakeAsync(id, cancellationToken);
    return intake is null
        ? ApiErrors.NotFound("Intake")
        : Results.Ok(FhirStyleExportMapper.ToExport(intake));
})
    .WithName("GetFhirStyleExport")
    .WithTags("Interoperability");

app.Run();

public partial class Program;
