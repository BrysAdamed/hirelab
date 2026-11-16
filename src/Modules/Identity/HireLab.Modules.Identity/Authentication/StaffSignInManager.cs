using System.Security.Claims;
using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Authentication;

internal sealed class StaffSignInManager : SignInManager<User>
{
    public StaffSignInManager(
        UserManager<User> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<User> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<User>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<User> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        AuthenticationScheme = StaffAuthentication.Scheme;
    }

    public override async Task<bool> CanSignInAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.IsActive && await base.CanSignInAsync(user);
    }

    public override Task SignOutAsync() => Context.SignOutAsync(AuthenticationScheme);

    public override async Task<User?> ValidateSecurityStampAsync(ClaimsPrincipal? principal) =>
        await base.ValidateSecurityStampAsync(principal) is { IsActive: true } user ? user : null;
}
