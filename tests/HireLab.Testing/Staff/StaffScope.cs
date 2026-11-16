using System.Security.Claims;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Testing.Staff;

/// <summary>
/// A service scope of the API that runs as a signed-in staff user, as a request with their cookie would: their
/// <see cref="ICurrentUser"/> and team query filter apply. Use it to call module services and contracts directly.
/// Create it with <see cref="HireLabTestHost.CreateScopeAs"/> and dispose it before the next request.
/// </summary>
public sealed class StaffScope : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly IHttpContextAccessor _httpContextAccessor;

    internal StaffScope(IServiceProvider services, TestUser user)
    {
        _scope = services.CreateAsyncScope();
        _httpContextAccessor = _scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        List<Claim> claims =
        [
            new(StaffClaimTypes.UserId, user.Id.ToString()),
            new(StaffClaimTypes.Role, user.Role.ToString()),
        ];
        if (user.TeamId is { } teamId)
        {
            claims.Add(new Claim(StaffClaimTypes.TeamId, teamId.ToString()));
        }

        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, StaffAuthentication.Scheme)),
        };
    }

    /// <summary>The scope's services.</summary>
    public IServiceProvider Services => _scope.ServiceProvider;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _httpContextAccessor.HttpContext = null;
        return _scope.DisposeAsync();
    }
}
