using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.Domain;

/// <summary>
/// Whether a candidate can still be interviewed (ADR 0031): planning a session and going Live need a candidate of the
/// team whose position is open and who isn't anonymised. The position is checked first, since closing it anonymises
/// its candidates and the closed position is the clearer reason.
/// </summary>
internal static class CandidateEligibility
{
    public static Result Check(CandidateSummary? candidate)
    {
        if (candidate is null)
        {
            return InterviewSessionErrors.CandidateNotFound;
        }

        if (candidate.IsJobPositionClosed)
        {
            return InterviewSessionErrors.PositionClosed;
        }

        return candidate.IsAnonymised ? InterviewSessionErrors.CandidateAnonymised : Result.Success();
    }
}
