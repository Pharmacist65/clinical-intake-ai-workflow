using Microsoft.AspNetCore.Http;

namespace ClinicalIntake.Api.Contracts;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    IReadOnlyList<ApiValidationError> Errors);

public sealed record ApiValidationError(string Field, string Message);

public sealed class ApiValidationResult
{
    private readonly List<ApiValidationError> _errors = [];

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<ApiValidationError> Errors => _errors;

    public void Add(string field, string message) =>
        _errors.Add(new ApiValidationError(field, message));
}

public static class ApiErrors
{
    public static IResult Validation(ApiValidationResult validation) =>
        Results.BadRequest(new ApiErrorResponse(
            "validation_error",
            "Request validation failed.",
            validation.Errors));

    public static IResult NotFound(string resourceName) =>
        Results.NotFound(new ApiErrorResponse(
            "not_found",
            $"{resourceName} not found.",
            []));

    public static ApiErrorResponse Unexpected() =>
        new(
            "server_error",
            "An unexpected error occurred.",
            []);
}
