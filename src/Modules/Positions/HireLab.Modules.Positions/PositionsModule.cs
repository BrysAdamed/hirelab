using HireLab.Modules.Positions.Attachments;
using HireLab.Modules.Positions.Attachments.Storage;
using HireLab.Modules.Positions.Candidates;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.JobPositions;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HireLab.Modules.Positions;

/// <summary>The Positions module's registration entry point.</summary>
public sealed class PositionsModule : IModule, IModuleMigrator
{
    /// <summary>
    /// The private blob container that holds candidate attachments (ADR 0005). The host registers a
    /// <c>BlobServiceClient</c> for the storage account and provisions the container.
    /// </summary>
    public const string AttachmentsContainerName = "attachments";

    /// <inheritdoc />
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModuleDbContext<PositionsDbContext>();
        builder.Services.AddHandlers<PositionsDbContext>(typeof(PositionsModule).Assembly);
        builder.Services.AddScoped<ICandidateDirectory, CandidateDirectory>();
        builder.Services.AddSingleton<IAttachmentStore, BlobAttachmentStore>();
        builder.Services.AddSingleton<AttachmentBlobCleanup>();
    }

    /// <inheritdoc />
    public void MapStaffEndpoints(IEndpointRouteBuilder staff)
    {
        staff.MapJobPositionEndpoints();
        staff.MapCandidateEndpoints();
        staff.MapAttachmentEndpoints();
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services) =>
        services.AddModuleDbContext<PositionsDbContext>();

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<PositionsDbContext>().Database.MigrateAsync(cancellationToken);
}
