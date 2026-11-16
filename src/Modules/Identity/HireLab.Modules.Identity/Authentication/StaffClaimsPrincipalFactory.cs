using System.Security.Claims;
using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Authentication;

internal sealed class StaffClaimsPrincipalFactory(UserManager<User> userManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<User>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(StaffClaimTypes.Role, user.Role.ToString()));
        if (user.TeamId is { } teamId)
        {
            identity.AddClaim(new Claim(StaffClaimTypes.TeamId, teamId.ToString()));
        }

        return identity;
    }
}
