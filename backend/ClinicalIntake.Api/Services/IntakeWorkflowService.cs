using System.Text.Json;
using ClinicalIntake.Api.Contracts;
using ClinicalIntake.Api.Data;
using ClinicalIntake.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicalIntake.Api.Services;

public sealed class IntakeWorkflowService(
    AppDbContext db,
    IAiSummaryService aiSummaryService,
    MedicationContextService medicationContextService)
{
    public async Task<Intake> CreateIntakeAsync(CreateIntakeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = IntakeRequestValidator.ValidateCreate(request);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Errors[0].Message);
        }

        var intake = new Intake
        {
            PatientAlias = request.PatientAlias.Trim(),
            Age = request.Age,
            IntakeText = request.IntakeText.Trim(),
            Source = request.Source.Trim(),
            CreatedBy = request.CreatedBy.Trim(),
            CreatedAt = DateTime.UtcNow,
            ReviewStatus = ReviewStatus.New
        };

        intake.AuditLogs.Add(new AuditLog
        {
            Action = "IntakeCreated",
            Actor = intake.CreatedBy,
            Timestamp = DateTime.UtcNow,
            Details = "Intake was created and queued for summary generation."
        });

        db.Intakes.Add(intake);
        await db.SaveChangesAsync(cancellationToken);
        return await GetRequiredIntakeAsync(intake.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<Intake>> ListIntakesAsync(CancellationToken cancellationToken = default) =>
        await db.Intakes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(intake => intake.RiskFlags)
            .Include(intake => intake.MedicationSignals)
            .OrderByDescending(intake => intake.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Intake?> GetIntakeAsync(int id, CancellationToken cancellationToken = default) =>
        await QueryFullIntake()
            .AsNoTracking()
            .FirstOrDefaultAsync(intake => intake.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Intake>> GetReviewQueueAsync(CancellationToken cancellationToken = default) =>
        await db.Intakes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(intake => intake.RiskFlags)
            .Include(intake => intake.MedicationSignals)
            .Where(intake => intake.ReviewStatus == ReviewStatus.NeedsReview)
            .OrderByDescending(intake => intake.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLog>?> GetAuditLogsAsync(int intakeId, CancellationToken cancellationToken = default)
    {
        var exists = await db.Intakes.AnyAsync(intake => intake.Id == intakeId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await db.AuditLogs
            .AsNoTracking()
            .Where(log => log.IntakeId == intakeId)
            .OrderBy(log => log.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContextEvent?> AddContextEventAsync(
        int intakeId,
        CreateContextEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = IntakeRequestValidator.ValidateContextEvent(request);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Errors[0].Message);
        }

        var intake = await db.Intakes
            .Include(existing => existing.AuditLogs)
            .FirstOrDefaultAsync(existing => existing.Id == intakeId, cancellationToken);

        if (intake is null)
        {
            return null;
        }

        var contextEvent = new ContextEvent
        {
            IntakeId = intakeId,
            SourceType = Enum.Parse<ContextSourceType>(request.SourceType, ignoreCase: true),
            SourceLabel = request.SourceLabel.Trim(),
            Content = request.Content.Trim(),
            CapturedAt = request.CapturedAt ?? DateTime.UtcNow,
            CreatedBy = request.CreatedBy.Trim(),
            ConfidenceScore = request.ConfidenceScore,
            MetadataJson = CleanOptional(request.MetadataJson),
            CreatedAt = DateTime.UtcNow
        };

        db.ContextEvents.Add(contextEvent);
        intake.AuditLogs.Add(new AuditLog
        {
            Action = "ContextEventAdded",
            Actor = contextEvent.CreatedBy,
            Timestamp = DateTime.UtcNow,
            Details = $"Context event recorded from {contextEvent.SourceType}: {contextEvent.SourceLabel}."
        });

        await db.SaveChangesAsync(cancellationToken);
        return contextEvent;
    }

    public async Task<IReadOnlyList<ContextEvent>?> ListContextEventsAsync(
        int intakeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Intakes.AnyAsync(intake => intake.Id == intakeId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await db.ContextEvents
            .AsNoTracking()
            .Where(contextEvent => contextEvent.IntakeId == intakeId)
            .OrderByDescending(contextEvent => contextEvent.CapturedAt)
            .ThenByDescending(contextEvent => contextEvent.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContextEvent?> AddTranscriptContextAsync(
        int intakeId,
        CreateTranscriptContextRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = IntakeRequestValidator.ValidateTranscriptContext(request);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Errors[0].Message);
        }

        var intake = await db.Intakes
            .Include(existing => existing.AuditLogs)
            .FirstOrDefaultAsync(existing => existing.Id == intakeId, cancellationToken);

        if (intake is null)
        {
            return null;
        }

        var contextEvent = new ContextEvent
        {
            IntakeId = intakeId,
            SourceType = ContextSourceType.TranscriptText,
            SourceLabel = request.TranscriptLabel.Trim(),
            Content = request.TranscriptText.Trim(),
            CapturedAt = request.CapturedAt ?? DateTime.UtcNow,
            CreatedBy = request.CreatedBy.Trim(),
            ConfidenceScore = request.ConfidenceScore,
            MetadataJson = BuildTranscriptMetadata(request.SpeakerContext),
            CreatedAt = DateTime.UtcNow
        };

        db.ContextEvents.Add(contextEvent);
        intake.AuditLogs.Add(new AuditLog
        {
            Action = "TranscriptContextAdded",
            Actor = contextEvent.CreatedBy,
            Timestamp = DateTime.UtcNow,
            Details = $"Mock transcript context recorded for workflow support: {contextEvent.SourceLabel}."
        });

        await db.SaveChangesAsync(cancellationToken);
        return contextEvent;
    }

    public async Task<MedicationEntry?> AddMedicationAsync(
        int intakeId,
        CreateMedicationEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = IntakeRequestValidator.ValidateMedication(request);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Errors[0].Message);
        }

        var intake = await db.Intakes
            .Include(existing => existing.AuditLogs)
            .FirstOrDefaultAsync(existing => existing.Id == intakeId, cancellationToken);

        if (intake is null)
        {
            return null;
        }

        var medication = new MedicationEntry
        {
            IntakeId = intakeId,
            MedicationName = request.MedicationName.Trim(),
            NormalizedName = NormalizeMedicationName(request.MedicationName),
            Category = Enum.Parse<MedicationCategory>(request.Category, ignoreCase: true),
            Dose = CleanOptional(request.Dose),
            Route = CleanOptional(request.Route),
            Frequency = CleanOptional(request.Frequency),
            StartedAt = request.StartedAt,
            StoppedAt = request.StoppedAt,
            ReasonForUse = CleanOptional(request.ReasonForUse),
            Source = Enum.Parse<MedicationSource>(request.Source, ignoreCase: true),
            PrescribedBy = CleanOptional(request.PrescribedBy),
            Notes = CleanOptional(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        db.MedicationEntries.Add(medication);
        intake.AuditLogs.Add(new AuditLog
        {
            Action = "MedicationEntryAdded",
            Actor = "MedicationContext",
            Timestamp = DateTime.UtcNow,
            Details = $"Medication context recorded for {medication.MedicationName}."
        });

        await db.SaveChangesAsync(cancellationToken);
        return medication;
    }

    public async Task<IReadOnlyList<MedicationEntry>?> ListMedicationsAsync(
        int intakeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Intakes.AnyAsync(intake => intake.Id == intakeId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await db.MedicationEntries
            .AsNoTracking()
            .Where(medication => medication.IntakeId == intakeId)
            .OrderByDescending(medication => medication.StartedAt ?? medication.CreatedAt)
            .ThenBy(medication => medication.MedicationName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicationSignal>?> GetMedicationSignalsAsync(
        int intakeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Intakes.AnyAsync(intake => intake.Id == intakeId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await db.MedicationSignals
            .AsNoTracking()
            .Where(signal => signal.IntakeId == intakeId)
            .OrderByDescending(signal => signal.Severity)
            .ThenBy(signal => signal.Label)
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicationDocumentationQuality?> GetMedicationDocumentationQualityAsync(
        int intakeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Intakes.AnyAsync(intake => intake.Id == intakeId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var medications = await db.MedicationEntries
            .AsNoTracking()
            .Where(medication => medication.IntakeId == intakeId)
            .ToListAsync(cancellationToken);

        return MedicationContextService.AssessDocumentationQuality(medications);
    }

    public async Task<Intake?> AnalyseMedicationContextAsync(
        int intakeId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var intake = await QueryFullIntake()
            .FirstOrDefaultAsync(existing => existing.Id == intakeId, cancellationToken);

        if (intake is null)
        {
            return null;
        }

        if (intake.MedicationSignals.Count > 0)
        {
            db.MedicationSignals.RemoveRange(intake.MedicationSignals);
            intake.MedicationSignals.Clear();
        }

        var signals = medicationContextService.Analyse(intake);
        intake.MedicationSignals.AddRange(signals);

        var highSeveritySignalExists = signals.Any(signal => signal.Severity == RiskSeverity.High);
        if (highSeveritySignalExists)
        {
            intake.ReviewStatus = ReviewStatus.NeedsReview;
        }

        intake.AuditLogs.Add(new AuditLog
        {
            Action = "MedicationContextAnalysed",
            Actor = actor,
            Timestamp = DateTime.UtcNow,
            Details = highSeveritySignalExists
                ? "Medication context analysed and high-severity review signal routed to human review."
                : "Medication context analysed for workflow support signals."
        });

        await db.SaveChangesAsync(cancellationToken);
        return await GetRequiredIntakeAsync(intakeId, cancellationToken);
    }

    public async Task<Intake?> GenerateSummaryAsync(
        int intakeId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var intake = await QueryFullIntake()
            .FirstOrDefaultAsync(existing => existing.Id == intakeId, cancellationToken);

        if (intake is null)
        {
            return null;
        }

        if (intake.RiskFlags.Count > 0)
        {
            db.RiskFlags.RemoveRange(intake.RiskFlags);
            intake.RiskFlags.Clear();
        }

        var result = aiSummaryService.Generate(intake);
        ApplySummary(intake, result.Summary);
        intake.RiskFlags.AddRange(result.RiskFlags);

        var requiresReview = result.Summary.ConfidenceScore < 0.75m
            || result.RiskFlags.Any(flag => flag.Severity == RiskSeverity.High)
            || intake.MedicationSignals.Any(signal => signal.Severity == RiskSeverity.High);
        intake.ReviewStatus = requiresReview ? ReviewStatus.NeedsReview : ReviewStatus.New;

        intake.AuditLogs.Add(new AuditLog
        {
            Action = "AiSummaryGenerated",
            Actor = actor,
            Timestamp = DateTime.UtcNow,
            Details = requiresReview
                ? "Mock AI summary generated and intake routed to human review."
                : "Mock AI summary generated for routine human review."
        });

        await db.SaveChangesAsync(cancellationToken);
        return await GetRequiredIntakeAsync(intakeId, cancellationToken);
    }

    public async Task<Intake?> UpdateReviewStatusAsync(
        int intakeId,
        ReviewStatus reviewStatus,
        string actor,
        string? reviewNote = null,
        CancellationToken cancellationToken = default)
    {
        var intake = await db.Intakes
            .Include(existing => existing.AuditLogs)
            .FirstOrDefaultAsync(existing => existing.Id == intakeId, cancellationToken);

        if (intake is null)
        {
            return null;
        }

        var previousStatus = intake.ReviewStatus;
        var cleanedNote = CleanOptional(reviewNote);
        intake.ReviewStatus = reviewStatus;
        intake.AuditLogs.Add(new AuditLog
        {
            Action = "ReviewStatusUpdated",
            Actor = actor,
            Timestamp = DateTime.UtcNow,
            Details = cleanedNote is null
                ? $"Review status changed from {previousStatus} to {reviewStatus}."
                : $"Review status changed from {previousStatus} to {reviewStatus}. Reviewer note: {cleanedNote}"
        });

        await db.SaveChangesAsync(cancellationToken);
        return await GetRequiredIntakeAsync(intakeId, cancellationToken);
    }

    private IQueryable<Intake> QueryFullIntake() =>
        db.Intakes
            .AsSplitQuery()
            .Include(intake => intake.AiSummary)
            .Include(intake => intake.RiskFlags)
            .Include(intake => intake.ContextEvents)
            .Include(intake => intake.MedicationEntries)
            .Include(intake => intake.MedicationSignals)
            .Include(intake => intake.AuditLogs);

    private async Task<Intake> GetRequiredIntakeAsync(int id, CancellationToken cancellationToken) =>
        await QueryFullIntake().FirstAsync(intake => intake.Id == id, cancellationToken);

    private static void ApplySummary(Intake intake, AiSummary generatedSummary)
    {
        if (intake.AiSummary is null)
        {
            intake.AiSummary = generatedSummary;
            return;
        }

        intake.AiSummary.PresentingConcerns = generatedSummary.PresentingConcerns;
        intake.AiSummary.RelevantHistory = generatedSummary.RelevantHistory;
        intake.AiSummary.PossibleRisks = generatedSummary.PossibleRisks;
        intake.AiSummary.RecommendedNextStep = generatedSummary.RecommendedNextStep;
        intake.AiSummary.ConfidenceScore = generatedSummary.ConfidenceScore;
        intake.AiSummary.GeneratedAt = generatedSummary.GeneratedAt;
        intake.AiSummary.Disclaimer = generatedSummary.Disclaimer;
    }

    private static string NormalizeMedicationName(string medicationName) =>
        string.Join(" ", medicationName.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BuildTranscriptMetadata(string? speakerContext)
    {
        var cleanedSpeakerContext = CleanOptional(speakerContext);
        if (cleanedSpeakerContext is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new
        {
            mode = "mock-transcript",
            speakerContext = cleanedSpeakerContext,
            safetyScope = "workflow-support-only-no-diagnosis"
        });
    }
}
