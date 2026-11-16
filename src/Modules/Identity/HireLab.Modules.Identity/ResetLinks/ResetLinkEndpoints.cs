using HireLab.Modules.Identity.ResetLinks.GenerateResetLink;
using HireLab.Modules.Identity.ResetLinks.Redemption;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.ResetLinks;

internal static class ResetLinkEndpoints
{
    public const string Tag = "ResetLinks";

    private const string GenerateSummary =
        "Generates a reset link for the user. Revokes the user's previous unused link and returns the new one, which is shown only once.";

    public static void MapResetLinkEndpoints(this IEndpointRouteBuilder staff)
    {
        var resetLinks = staff.MapGroup("/reset-links").WithTags(Tag);

        resetLinks.MapGroup("/managers")
            .RequireAuthorization(StaffPolicies.Admin)
            .MapGenerateResetLink("GenerateManagerResetLink", _ => StaffScope.Managers)
            .WithSummary(GenerateSummary);

        resetLinks.MapGroup("/interviewers")
            .RequireAuthorization(StaffPolicies.Manager)
            .MapGenerateResetLink("GenerateInterviewerResetLink", StaffScope.InterviewersOf)
            .WithSummary(GenerateSummary);

        resetLinks.MapResetLinkRedemption();
    }
}
