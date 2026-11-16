using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Positions.Attachments.OrphanSweep;

internal static class OrphanAttachmentSweepRegistration
{
    public static IServiceCollection AddOrphanAttachmentSweep(this IServiceCollection services)
    {
        services.AddOptions<OrphanAttachmentSweepOptions>().BindConfiguration(OrphanAttachmentSweepOptions.Section).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<OrphanAttachmentSweepOptions>, OrphanAttachmentSweepOptionsValidator>());
        services.AddScoped<OrphanAttachmentSweeper>();
        services.AddHostedService<OrphanAttachmentSweep>();
        return services;
    }
}
