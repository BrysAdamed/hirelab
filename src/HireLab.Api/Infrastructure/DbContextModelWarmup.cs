using HireLab.Api.OpenApi;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Api.Infrastructure;

/// <summary>
/// Builds every module DbContext's model while the host starts, before it listens and reports healthy. Otherwise
/// the first requests that touch the database all wait on EF Core's model-building lock, which on a cold, busy
/// machine takes long enough for clients to time out.
/// </summary>
internal sealed class DbContextModelWarmup(IServiceScopeFactory scopeFactory, IReadOnlyList<Type> contextTypes)
    : IHostedService
{
    public IReadOnlyList<Type> ContextTypes { get; } = contextTypes;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        foreach (var contextType in ContextTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = ((DbContext)scope.ServiceProvider.GetRequiredService(contextType)).Model;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class DbContextModelWarmupExtensions
{
    /// <summary>
    /// Warms up the module DbContexts registered so far; call it after the modules add their services. Skipped while the
    /// build generates the OpenAPI documents, which runs the host without a database.
    /// </summary>
    public static IServiceCollection AddDbContextModelWarmup(this IServiceCollection services)
    {
        if (BuildTimeDocumentGeneration.IsRunning)
        {
            return services;
        }

        Type[] contextTypes =
        [
            .. services
                .Select(descriptor => descriptor.ServiceType)
                .Where(type => typeof(DbContext).IsAssignableFrom(type) && typeof(IModuleDbContext).IsAssignableFrom(type))
                .Distinct(),
        ];

        return services.AddHostedService(provider =>
            new DbContextModelWarmup(provider.GetRequiredService<IServiceScopeFactory>(), contextTypes));
    }
}
