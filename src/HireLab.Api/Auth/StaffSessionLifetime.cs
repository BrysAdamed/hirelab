using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace HireLab.Api.Auth;

internal static class StaffSessionLifetime
{
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(8);
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(7);

    private const string StartedAtItem = ".hirelab.session_started";

    public static Task OnSigningInAsync(CookieSigningInContext context)
    {
        context.Properties.Items.TryAdd(StartedAtItem, UtcNow(context.HttpContext).ToString("O", CultureInfo.InvariantCulture));
        return Task.CompletedTask;
    }

    public static async Task OnValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        if (HasReachedAbsoluteLifetime(context))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
            return;
        }

        await SecurityStampValidator.ValidatePrincipalAsync(context);
    }

    private static bool HasReachedAbsoluteLifetime(CookieValidatePrincipalContext context)
    {
        if (!context.Properties.Items.TryGetValue(StartedAtItem, out var value)
            || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var startedAt))
        {
            return true;
        }

        return UtcNow(context.HttpContext) - startedAt >= AbsoluteLifetime;
    }

    private static DateTimeOffset UtcNow(HttpContext context) =>
        context.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
}
