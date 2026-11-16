using HireLab.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HireLab.Modules.Identity.Authentication;

internal static class StaffIdentityRegistration
{
    public const int MinimumPasswordLength = 12;
    public const int MaxFailedAccessAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan SecurityStampValidationInterval = TimeSpan.FromMinutes(5);

    public static IdentityBuilder AddStaffIdentityCore(this IServiceCollection services) =>
        services.AddIdentityCore<User>(ConfigureIdentity)
            .AddUserStore<StaffUserStore>()
            .AddClaimsPrincipalFactory<StaffClaimsPrincipalFactory>();

    public static IdentityBuilder AddStaffSignIn(this IdentityBuilder identity)
    {
        identity.AddSignInManager<StaffSignInManager>();
        identity.Services.Replace(ServiceDescriptor.Scoped<ISecurityStampValidator, StaffSecurityStampValidator>());

        identity.Services.AddOptions<SecurityStampValidatorOptions>()
            .Configure<TimeProvider>((options, timeProvider) =>
            {
                options.ValidationInterval = SecurityStampValidationInterval;
                options.TimeProvider = timeProvider;
            });

        return identity;
    }

    private static void ConfigureIdentity(IdentityOptions options)
    {
        options.Password.RequiredLength = MinimumPasswordLength;
        options.Password.RequiredUniqueChars = 1;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = string.Empty;
    }
}
