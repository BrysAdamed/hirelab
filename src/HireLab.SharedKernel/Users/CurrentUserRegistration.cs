using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HireLab.SharedKernel.Users;

/// <summary>Registers <see cref="ICurrentUser"/>.</summary>
public static class CurrentUserRegistration
{
    /// <summary>Registers a scoped <see cref="ICurrentUser"/> read from the current request's claims.</summary>
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, ClaimsCurrentUser>();

        return services;
    }
}
