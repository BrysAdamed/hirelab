using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Positions.Domain;

internal static class CandidateErrors
{
    public static readonly Error NotFound = Error.NotFound("Candidate.NotFound", "The candidate was not found.");

    public static readonly Error Anonymised = Error.Conflict(
        "Candidate.Anonymised",
        "This candidate has been anonymised. Their details can no longer be changed.");

    public static readonly Error UnfinishedSessions = Error.Conflict(
        "Candidate.UnfinishedSessions",
        "This candidate has an interview session that isn't finished yet. Finish it before anonymising the candidate.");
}
