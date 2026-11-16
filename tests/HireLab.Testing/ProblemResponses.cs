using System.Net;
using System.Net.Http.Json;
using HireLab.SharedKernel.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace HireLab.Testing;

/// <summary>Reads and asserts the RFC 9457 <see cref="ProblemDetails"/> the API answers with on an expected failure.</summary>
public static class ProblemResponses
{
    public static async Task<ProblemDetails> ProblemAsync(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    /// <summary>The error code in the <c>code</c> extension member, or <c>null</c>.</summary>
    public static string? Code(this ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return problem.Extensions.TryGetValue(ErrorProblemDetails.CodeExtension, out var code) ? code?.ToString() : null;
    }

    /// <summary>Asserts the response's status code and the error code of its ProblemDetails.</summary>
    public static async Task ShouldBeProblemAsync(this HttpResponseMessage response, HttpStatusCode status, string code)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.StatusCode.ShouldBe(status);
        (await response.ProblemAsync()).Code().ShouldBe(code);
    }
}
