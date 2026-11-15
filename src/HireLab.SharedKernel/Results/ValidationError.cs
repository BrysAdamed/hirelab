namespace HireLab.SharedKernel.Results;

/// <summary>
/// A validation failure with the messages for each invalid field, keyed by the field name.
/// </summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error(ValidationError.DefaultCode, "One or more validation errors occurred.", ErrorType.Validation)
{
    public const string DefaultCode = "Validation.Failed";
}
