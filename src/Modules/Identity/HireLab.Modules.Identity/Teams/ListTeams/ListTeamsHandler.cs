using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Teams.ListTeams;

internal sealed record ListTeamsQuery;

/// <summary>A team with its active manager, if any, and all its members, active and deactivated.</summary>
internal sealed record TeamResponse(Guid Id, string Name, UserResponse? Manager, IReadOnlyList<UserResponse> Members);

/// <summary>Lists every team by name. Members are listed managers first, then by display name.</summary>
internal sealed class ListTeamsHandler(IdentityModuleDbContext dbContext) : IQueryHandler<ListTeamsQuery, Result<IReadOnlyList<TeamResponse>>>
{
    public async Task<Result<IReadOnlyList<TeamResponse>>> HandleAsync(ListTeamsQuery query, CancellationToken cancellationToken)
    {
        var teams = await dbContext.Teams.AsNoTracking()
            .OrderBy(team => team.Name)
            .ThenBy(team => team.Id)
            .Select(team => new { team.Id, team.Name })
            .ToListAsync(cancellationToken);

        var members = await dbContext.Users.AsNoTracking()
            .Where(user => user.TeamId != null)
            .ToResponses(dbContext)
            .ToListAsync(cancellationToken);
        var membersByTeam = members.ToLookup(member => member.Team!.Id);

        return teams
            .Select(team =>
            {
                var teamMembers = membersByTeam[team.Id.Value]
                    .OrderBy(member => member.Role == StaffRole.Manager ? 0 : 1)
                    .ToList();
                var manager = teamMembers.SingleOrDefault(member => member is { Role: StaffRole.Manager, IsActive: true });
                return new TeamResponse(team.Id.Value, team.Name, manager, teamMembers);
            })
            .ToList();
    }
}
