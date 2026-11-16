using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Bootstrap;

internal static class AdminBootstrapRegistration
{
    public static IServiceCollection AddAdminBootstrap(this IServiceCollection services)
    {
        services.AddOptions<AdminBootstrapOptions>().BindConfiguration(AdminBootstrapOptions.Section).ValidateOnStart();
        services.AddOptions<StaffAppOptions>().BindConfiguration(StaffAppOptions.Section).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AdminBootstrapOptions>, AdminBootstrapOptionsValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<StaffAppOptions>, AdminBootstrapOptionsValidator>());
        services.AddScoped<AdminBootstrap>();
        return services;
    }
}
