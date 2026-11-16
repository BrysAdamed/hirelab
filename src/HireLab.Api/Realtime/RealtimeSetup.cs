using HireLab.Api.Auth;
using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.SignalR;

namespace HireLab.Api.Realtime;

/// <summary>The <c>Realtime</c> component (ADR 0006): both SignalR hubs and the relay from integration events to them.</summary>
internal static class RealtimeSetup
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddHubOptions<StaffHub>(options => options.AddFilter<HubCallerHttpContextFilter>());

        services.AddSingleton<CandidateConnections>();
        services.AddScoped<IIntegrationEventHandler<TaskQueueChanged>, SessionEventsRelay>();
        services.AddScoped<IIntegrationEventHandler<SessionStateChanged>, SessionEventsRelay>();
        services.AddScoped<IIntegrationEventHandler<ResponseUpdated>, SessionEventsRelay>();
        services.AddScoped<IIntegrationEventHandler<CandidateLinkRevoked>, SessionEventsRelay>();

        return services;
    }

    public static WebApplication MapRealtime(this WebApplication app)
    {
        app.MapHub<StaffHub>(StaffHub.Path)
            .RequireAuthorization(AuthPolicies.Staff, StaffPolicies.ManagerOrInterviewer);
        app.MapHub<CandidateHub>(CandidateHub.Path)
            .RequireAuthorization(AuthPolicies.Candidate);

        return app;
    }
}
