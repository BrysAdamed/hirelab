using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.Domain;

/// <summary>Why a candidate link or a candidate cookie gives no access (ADR 0008). The candidate app shows a page for each code.</summary>
internal static class CandidateAccessErrors
{
    public static readonly Error InvalidLink = Error.NotFound(
        "CandidateLink.Invalid",
        "This link doesn't work. It may have been replaced by a new one: ask your interviewer for the current link.");

    public static readonly Error Ended = Error.Conflict("CandidateSession.Ended", "This interview has ended.");
}
