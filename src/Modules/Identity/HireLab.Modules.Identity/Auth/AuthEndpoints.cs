using HireLab.Modules.Identity.Auth.Antiforgery;
using HireLab.Modules.Identity.Auth.ChangePassword;
using HireLab.Modules.Identity.Auth.CurrentUser;
using HireLab.Modules.Identity.Auth.Login;
using HireLab.Modules.Identity.Auth.Logout;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Auth;

internal static class AuthEndpoints
{
    public const string Tag = "Auth";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder staff)
    {
        var auth = staff.MapGroup("/auth").WithTags(Tag);

        auth.MapAntiforgeryToken();
        auth.MapLogin();
        auth.MapLogout();
        auth.MapCurrentUser();
        auth.MapChangePassword();
    }
}
