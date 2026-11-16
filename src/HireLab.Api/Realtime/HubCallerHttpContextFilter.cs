using Microsoft.AspNetCore.SignalR;

namespace HireLab.Api.Realtime;

/// <summary>
/// Makes the caller's connection the current <see cref="HttpContext"/> while a hub method runs. With WebSockets it
/// already is; with long polling and server-sent events the request that started the connection has ended, so without
/// this <c>ICurrentUser</c>, and with it the team query filter, would see no signed-in user.
/// </summary>
internal sealed class HubCallerHttpContextFilter(IHttpContextAccessor httpContextAccessor) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (httpContextAccessor.HttpContext is null)
        {
            httpContextAccessor.HttpContext = invocationContext.Context.GetHttpContext();
        }

        return await next(invocationContext);
    }
}
