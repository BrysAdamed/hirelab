using HireLab.Modules.Positions.Candidates.AddCandidate;
using HireLab.Modules.Positions.Candidates.AnonymiseCandidate;
using HireLab.Modules.Positions.Candidates.EditCandidate;
using HireLab.Modules.Positions.Candidates.GetCandidate;
using HireLab.Modules.Positions.Candidates.ListCandidates;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Candidates;

internal static class CandidateEndpoints
{
    public const string Tag = "Candidates";

    public static void MapCandidateEndpoints(this IEndpointRouteBuilder staff)
    {
        var positionCandidates = staff.MapGroup("/positions/{positionId:guid}/candidates")
            .WithTags(Tag)
            .RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
        positionCandidates.MapListCandidates();
        positionCandidates.MapAddCandidate();

        var candidates = staff.MapGroup("/candidates").WithTags(Tag).RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
        candidates.MapGetCandidate();
        candidates.MapEditCandidate();

        var managed = candidates.MapGroup(string.Empty).RequireAuthorization(StaffPolicies.Manager);
        managed.MapAnonymiseCandidate();
    }
}
