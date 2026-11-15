namespace HireLab.SharedKernel.Results;

/// <summary>
/// An expected failure: a stable machine-readable <see cref="Code"/> (for example <c>JobPosition.Closed</c>),
/// a human-readable <see cref="Description"/> and the <see cref="Type"/> that decides the HTTP status code.
/// </summary>
public record Error(string Code, string Description, ErrorType Type)
{
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}
