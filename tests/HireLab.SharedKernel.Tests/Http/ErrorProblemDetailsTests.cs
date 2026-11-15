using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HireLab.SharedKernel.Tests.Http;

public sealed class ErrorProblemDetailsTests
{
    public static TheoryData<ErrorType, int> StatusCodesByType => new()
    {
        { ErrorType.Validation, StatusCodes.Status400BadRequest },
        { ErrorType.NotFound, StatusCodes.Status404NotFound },
        { ErrorType.Forbidden, StatusCodes.Status403Forbidden },
        { ErrorType.Conflict, StatusCodes.Status409Conflict },
    };

    [Theory]
    [MemberData(nameof(StatusCodesByType))]
    public void Error_maps_to_problem_details_with_status_title_detail_and_code(ErrorType type, int expectedStatus)
    {
        // Arrange
        var error = new Error("JobPosition.Closed", "The job position is closed.", type);

        // Act
        var problem = error.ToProblem();

        // Assert
        problem.StatusCode.ShouldBe(expectedStatus);
        problem.ProblemDetails.Status.ShouldBe(expectedStatus);
        problem.ProblemDetails.Title.ShouldNotBeNullOrWhiteSpace();
        problem.ProblemDetails.Type.ShouldNotBeNullOrWhiteSpace();
        problem.ProblemDetails.Detail.ShouldBe("The job position is closed.");
        problem.ProblemDetails.Extensions[ErrorProblemDetails.CodeExtension].ShouldBe("JobPosition.Closed");
        problem.ContentType.ShouldBe("application/problem+json");
    }

    [Fact]
    public void Every_error_type_has_a_status_code()
    {
        // Act & Assert
        foreach (var type in Enum.GetValues<ErrorType>())
        {
            Should.NotThrow(() => ErrorProblemDetails.StatusCodeFor(type));
        }
    }

    [Fact]
    public void Validation_error_maps_to_validation_problem_details_with_field_errors()
    {
        // Arrange
        var error = new ValidationError(new Dictionary<string, string[]>
        {
            ["Title"] = ["'Title' must not be empty."],
        });

        // Act
        var problem = error.ToProblem();

        // Assert
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var details = problem.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
        details.Errors["Title"].ShouldBe(["'Title' must not be empty."]);
        details.Extensions[ErrorProblemDetails.CodeExtension].ShouldBe(ValidationError.DefaultCode);
    }

    [Fact]
    public void Successful_result_maps_to_the_success_response()
    {
        // Arrange
        var result = Result.Success("Note");

        // Act
        var response = result.ToHttpResult(TypedResults.Ok);

        // Assert
        response.Result.ShouldBeOfType<Ok<string>>().Value.ShouldBe("Note");
    }

    [Fact]
    public void Failed_result_maps_to_problem_details()
    {
        // Arrange
        Result<string> result = Error.NotFound("Note.NotFound", "The note was not found.");

        // Act
        var response = result.ToHttpResult(TypedResults.Ok);

        // Assert
        response.Result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Result_without_value_maps_to_no_content_or_problem_details()
    {
        // Act
        var success = Result.Success().ToHttpResult();
        var failure = Result.Failure(Error.Conflict("Session.NotLive", "The session is not live.")).ToHttpResult();

        // Assert
        success.Result.ShouldBeOfType<NoContent>();
        failure.Result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }
}
