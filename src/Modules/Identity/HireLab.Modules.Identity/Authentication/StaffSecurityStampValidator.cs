using HireLab.Modules.Identity.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Authentication;

internal sealed class StaffSecurityStampValidator(
    IOptions<SecurityStampValidatorOptions> options,
    SignInManager<User> signInManager,
    ILoggerFactory loggerFactory)
    : SecurityStampValidator<User>(options, signInManager, loggerFactory)
{
    public override async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.IssuedUtc is { } issuedUtc && TimeProvider.GetUtcNow() - issuedUtc <= Options.ValidationInterval)
        {
            return;
        }

        if (await VerifySecurityStamp(context.Principal) is { } user)
        {
            await SecurityStampVerified(user, context);
            return;
        }

        context.RejectPrincipal();
        await SignInManager.SignOutAsync();
    }
}
