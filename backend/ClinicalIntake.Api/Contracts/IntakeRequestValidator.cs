using System.Text.Json;
using ClinicalIntake.Api.Models;

namespace ClinicalIntake.Api.Contracts;

public static class IntakeRequestValidator
{
    public const int PatientAliasMaxLength = 120;
    public const int IntakeTextMaxLength = 8000;
    public const int SourceMaxLength = 80;
    public const int ActorMaxLength = 120;
    public const int ContextSourceLabelMaxLength = 120;
    public const int ContextContentMaxLength = 6000;
    public const int ContextMetadataMaxLength = 2000;
    public const int TranscriptSpeakerContextMaxLength = 500;
    public const int MedicationNameMaxLength = 160;
    public const int MedicationShortTextMaxLength = 120;
    public const int MedicationRouteMaxLength = 80;
    public const int MedicationReasonMaxLength = 500;
    public const int MedicationNotesMaxLength = 1000;
    public const int ReviewNoteMaxLength = 1000;
    public const int MinimumAge = 0;
    public const int MaximumAge = 120;

    public static ApiValidationResult ValidateCreate(CreateIntakeRequest request)
    {
        var validation = new ApiValidationResult();

        ValidateRequiredText(validation, nameof(request.PatientAlias), request.PatientAlias, PatientAliasMaxLength);
        ValidateRequiredText(validation, nameof(request.IntakeText), request.IntakeText, IntakeTextMaxLength);
        ValidateRequiredText(validation, nameof(request.Source), request.Source, SourceMaxLength);
        ValidateRequiredText(validation, nameof(request.CreatedBy), request.CreatedBy, ActorMaxLength);

        if (request.Age is < MinimumAge or > MaximumAge)
        {
            validation.Add(nameof(request.Age), $"Age must be between {MinimumAge} and {MaximumAge}.");
        }

        return validation;
    }

    public static ApiValidationResult ValidateContextEvent(CreateContextEventRequest request)
    {
        var validation = new ApiValidationResult();

        ValidateEnum<ContextSourceType>(validation, nameof(request.SourceType), request.SourceType);
        ValidateRequiredText(validation, nameof(request.SourceLabel), request.SourceLabel, ContextSourceLabelMaxLength);
        ValidateRequiredText(validation, nameof(request.Content), request.Content, ContextContentMaxLength);
        ValidateRequiredText(validation, nameof(request.CreatedBy), request.CreatedBy, ActorMaxLength);
        ValidateOptionalText(validation, nameof(request.MetadataJson), request.MetadataJson, ContextMetadataMaxLength);
        ValidateOptionalJson(validation, nameof(request.MetadataJson), request.MetadataJson);

        if (request.ConfidenceScore is < 0m or > 1m)
        {
            validation.Add(nameof(request.ConfidenceScore), "ConfidenceScore must be between 0 and 1 when provided.");
        }

        return validation;
    }

    public static ApiValidationResult ValidateTranscriptContext(CreateTranscriptContextRequest request)
    {
        var validation = new ApiValidationResult();

        ValidateRequiredText(validation, nameof(request.TranscriptLabel), request.TranscriptLabel, ContextSourceLabelMaxLength);
        ValidateRequiredText(validation, nameof(request.TranscriptText), request.TranscriptText, ContextContentMaxLength);
        ValidateRequiredText(validation, nameof(request.CreatedBy), request.CreatedBy, ActorMaxLength);
        ValidateOptionalText(validation, nameof(request.SpeakerContext), request.SpeakerContext, TranscriptSpeakerContextMaxLength);

        if (request.ConfidenceScore is < 0m or > 1m)
        {
            validation.Add(nameof(request.ConfidenceScore), "ConfidenceScore must be between 0 and 1 when provided.");
        }

        return validation;
    }

    public static ApiValidationResult ValidateMedication(CreateMedicationEntryRequest request)
    {
        var validation = new ApiValidationResult();

        ValidateRequiredText(validation, nameof(request.MedicationName), request.MedicationName, MedicationNameMaxLength);
        ValidateEnum<MedicationCategory>(validation, nameof(request.Category), request.Category);
        ValidateEnum<MedicationSource>(validation, nameof(request.Source), request.Source);
        ValidateOptionalText(validation, nameof(request.Dose), request.Dose, MedicationShortTextMaxLength);
        ValidateOptionalText(validation, nameof(request.Route), request.Route, MedicationRouteMaxLength);
        ValidateOptionalText(validation, nameof(request.Frequency), request.Frequency, MedicationShortTextMaxLength);
        ValidateOptionalText(validation, nameof(request.ReasonForUse), request.ReasonForUse, MedicationReasonMaxLength);
        ValidateOptionalText(validation, nameof(request.PrescribedBy), request.PrescribedBy, MedicationNameMaxLength);
        ValidateOptionalText(validation, nameof(request.Notes), request.Notes, MedicationNotesMaxLength);

        if (request.StartedAt is not null && request.StoppedAt is not null && request.StoppedAt < request.StartedAt)
        {
            validation.Add(nameof(request.StoppedAt), "StoppedAt cannot be earlier than StartedAt.");
        }

        return validation;
    }

    public static ApiValidationResult ValidateReviewStatus(UpdateReviewStatusRequest request)
    {
        var validation = new ApiValidationResult();

        if (string.IsNullOrWhiteSpace(request.ReviewStatus))
        {
            validation.Add(nameof(request.ReviewStatus), "Review status is required.");
        }
        else if (!Enum.TryParse<ReviewStatus>(request.ReviewStatus, ignoreCase: true, out _))
        {
            validation.Add(nameof(request.ReviewStatus), "Review status must be New, NeedsReview, or Reviewed.");
        }

        ValidateRequiredText(validation, nameof(request.Actor), request.Actor, ActorMaxLength);
        ValidateOptionalText(validation, nameof(request.ReviewNote), request.ReviewNote, ReviewNoteMaxLength);

        return validation;
    }

    private static void ValidateRequiredText(
        ApiValidationResult validation,
        string field,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validation.Add(field, $"{field} is required.");
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            validation.Add(field, $"{field} must be {maxLength} characters or fewer.");
        }
    }

    private static void ValidateOptionalText(
        ApiValidationResult validation,
        string field,
        string? value,
        int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            validation.Add(field, $"{field} must be {maxLength} characters or fewer.");
        }
    }

    private static void ValidateOptionalJson(
        ApiValidationResult validation,
        string field,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
        }
        catch (JsonException)
        {
            validation.Add(field, $"{field} must be valid JSON when provided.");
        }
    }

    private static void ValidateEnum<TEnum>(
        ApiValidationResult validation,
        string field,
        string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validation.Add(field, $"{field} is required.");
            return;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out _))
        {
            validation.Add(field, $"{field} has an unsupported value.");
        }
    }
}
