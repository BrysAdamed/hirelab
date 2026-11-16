using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.Domain;

internal static class ResponseErrors
{
    public const string Field = "response";

    public static readonly Error StaleRevision = Error.Conflict(
        "Response.StaleRevision",
        "Your answer was updated in another tab. The latest version has been loaded.");

    public static readonly Error TaskTypeNotRegistered = Error.Conflict(
        "Response.TaskTypeNotRegistered",
        "This task can't be answered here. Please tell your interviewer.");

    public static ValidationError TooLarge { get; } = new(new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [Field] = [$"The response is larger than {Response.MaxBytes / 1024 / 1024} MB."],
    });
}
