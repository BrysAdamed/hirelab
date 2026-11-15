using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HireLab.SharedKernel.Http;

/// <summary>
/// Turns a handler's <see cref="Result"/> into a typed minimal API response: the success result, or the
/// <see cref="ErrorProblemDetails">ProblemDetails</see> for its error.
/// </summary>
public static class ResultHttpExtensions
{
    public static Results<TSuccess, ProblemHttpResult> ToHttpResult<TValue, TSuccess>(
        this Result<TValue> result,
        Func<TValue, TSuccess> onSuccess)
        where TSuccess : IResult
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();
    }

    public static Results<NoContent, ProblemHttpResult> ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
