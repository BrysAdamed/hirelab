using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HireLab.SharedKernel.Users;

internal sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly Lazy<StaffIdentity?> _identity = new(() => StaffIdentity.From(httpContextAccessor.HttpContext?.User));

    public bool IsAuthenticated => _identity.Value is not null;

    public UserId Id => Required.Id;

    public StaffRole Role => Required.Role;

    public TeamId? TeamId => _identity.Value?.TeamId;

    private StaffIdentity Required =>
        _identity.Value ?? throw new InvalidOperationException("The current request has no signed-in staff user.");

    internal sealed record StaffIdentity(UserId Id, StaffRole Role, TeamId? TeamId)
    {
        public static StaffIdentity? From(ClaimsPrincipal? principal)
        {
            if (principal?.Identity?.IsAuthenticated != true
                || !UserId.TryParse(principal.FindFirstValue(StaffClaimTypes.UserId), null, out var id)
                || !Enum.TryParse<StaffRole>(principal.FindFirstValue(StaffClaimTypes.Role), out var role)
                || !Enum.IsDefined(role))
            {
                return null;
            }

            var teamId = Users.TeamId.TryParse(principal.FindFirstValue(StaffClaimTypes.TeamId), null, out var team) ? team : (TeamId?)null;

            return new StaffIdentity(id, role, teamId);
        }
    }
}
