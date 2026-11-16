using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Auth.CurrentUser;

internal static class CurrentUserEndpoint
{
    public static RouteHandlerBuilder MapCurrentUser(this IEndpointRouteBuilder auth) =>
        auth.MapGet("/me", HandleAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Returns the signed-in staff user: id, email, display name, role and team.");

    private static async Task<Results<Ok<CurrentUserResponse>, ProblemHttpResult>> HandleAsync(
        ICurrentUser currentUser,
        IQueryHandler<GetCurrentUserQuery, Result<CurrentUserResponse>> getCurrentUser,
        CancellationToken cancellationToken)
    {
        var result = await getCurrentUser.HandleAsync(new GetCurrentUserQuery(currentUser.Id), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }
}
