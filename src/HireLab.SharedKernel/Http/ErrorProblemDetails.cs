using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HireLab.SharedKernel.Http;

/// <summary>
/// Maps an <see cref="Error"/> to an RFC 9457 <see cref="ProblemDetails"/> response for minimal API endpoints.
/// The error code is sent in the <c>code</c> extension member.
/// </summary>
public static class ErrorProblemDetails
{
    public const string CodeExtension = "code";

    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown error type."),
    };

    public static ProblemDetails ToProblemDetails(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        ProblemDetails problemDetails = error is ValidationError validationError
            ? new HttpValidationProblemDetails(validationError.Errors.ToDictionary(StringComparer.Ordinal))
            : new ProblemDetails();

        problemDetails.Status = StatusCodeFor(error.Type);
        problemDetails.Title = TitleFor(error.Type);
        problemDetails.Detail = error.Description;
        problemDetails.Extensions[CodeExtension] = error.Code;

        return problemDetails;
    }

    public static ProblemHttpResult ToProblem(this Error error) => TypedResults.Problem(error.ToProblemDetails());

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "One or more validation errors occurred.",
        ErrorType.NotFound => "The resource was not found.",
        ErrorType.Forbidden => "The operation is not allowed.",
        ErrorType.Conflict => "The operation conflicts with the current state of the resource.",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown error type."),
    };
}
