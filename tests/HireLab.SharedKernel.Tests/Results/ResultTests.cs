using HireLab.SharedKernel.Results;

namespace HireLab.SharedKernel.Tests.Results;

public sealed class ResultTests
{
    private static readonly Error _notFound = Error.NotFound("Note.NotFound", "The note was not found.");

    [Fact]
    public void Value_converts_to_a_successful_result()
    {
        // Act
        Result<int> result = 42;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Error_converts_to_a_failed_result_whose_value_cannot_be_read()
    {
        // Act
        Result<int> result = _notFound;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_notFound);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Result_without_value_succeeds_or_fails()
    {
        // Act
        var success = Result.Success();
        Result failure = _notFound;

        // Assert
        success.IsSuccess.ShouldBeTrue();
        failure.Error.ShouldBe(_notFound);
    }
}
