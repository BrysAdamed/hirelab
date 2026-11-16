using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.TeamsWithoutManager;

internal sealed record TeamsWithoutManagerQuery;

internal sealed record TeamWithoutManagerResponse(Guid Id, string Name);

internal sealed class TeamsWithoutManagerHandler(IdentityModuleDbContext dbContext)
    : IQueryHandler<TeamsWithoutManagerQuery, Result<IReadOnlyList<TeamWithoutManagerResponse>>>
{
    public async Task<Result<IReadOnlyList<TeamWithoutManagerResponse>>> HandleAsync(
        TeamsWithoutManagerQuery query,
        CancellationToken cancellationToken)
    {
        var teams = await dbContext.Teams.AsNoTracking()
            .Where(team => !dbContext.Users.Any(user => user.TeamId == team.Id && user.Role == StaffRole.Manager && user.IsActive))
            .OrderBy(team => team.Name)
            .Select(team => new TeamWithoutManagerResponse(team.Id.Value, team.Name))
            .ToListAsync(cancellationToken);

        return teams;
    }
}
