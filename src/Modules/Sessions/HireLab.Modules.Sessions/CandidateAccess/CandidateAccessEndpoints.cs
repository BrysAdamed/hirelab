using HireLab.Modules.Sessions.CandidateAccess.ExchangeCandidateLink;
using HireLab.Modules.Sessions.CandidateAccess.GetInterview;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.CandidateAccess;

internal static class CandidateAccessEndpoints
{
    public const string Tag = "Interview";

    public static void MapCandidateAccessEndpoints(this IEndpointRouteBuilder candidate)
    {
        var interview = candidate.MapGroup(string.Empty).WithTags(Tag);
        interview.MapCandidateLinkExchange();
        interview.MapGetInterview();
    }
}
