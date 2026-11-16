using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Positions.Domain;

internal static class JobPositionErrors
{
    public static readonly Error NotFound = Error.NotFound("JobPosition.NotFound", "The job position was not found.");

    public static readonly Error Closed = Error.Conflict(
        "JobPosition.Closed",
        "This job position is closed. Closed positions are read-only.");

    public static readonly Error HasCandidates = Error.Conflict(
        "JobPosition.HasCandidates",
        "This job position has candidates, so it can't be deleted.");
}
