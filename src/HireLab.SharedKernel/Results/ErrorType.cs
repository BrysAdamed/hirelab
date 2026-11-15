namespace HireLab.SharedKernel.Results;

/// <summary>
/// The kind of an expected failure. It decides the HTTP status code of the <c>ProblemDetails</c> response.
/// </summary>
public enum ErrorType
{
    /// <summary>The request is malformed or breaks an input rule (400).</summary>
    Validation,

    /// <summary>The resource doesn't exist or isn't visible to the caller (404).</summary>
    NotFound,

    /// <summary>The caller may not perform the operation (403).</summary>
    Forbidden,

    /// <summary>The operation conflicts with the current state of the resource, such as an invalid state transition (409).</summary>
    Conflict,
}
