using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Auth.CurrentUser;

internal sealed record GetCurrentUserQuery(UserId UserId);

internal sealed class GetCurrentUserHandler(IdentityModuleDbContext dbContext)
    : IQueryHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    public static readonly Error NotFound = Error.NotFound("Auth.UserNotFound", "The signed-in user no longer exists.");

    public async Task<Result<CurrentUserResponse>> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == query.UserId, cancellationToken);
        if (user is null)
        {
            return NotFound;
        }

        var team = user.TeamId is { } teamId
            ? await dbContext.Teams.AsNoTracking().SingleAsync(team => team.Id == teamId, cancellationToken)
            : null;

        return new CurrentUserResponse(
            user.Id.Value,
            user.Email!,
            user.DisplayName,
            user.Role,
            team is null ? null : new CurrentUserTeamResponse(team.Id.Value, team.Name));
    }
}
