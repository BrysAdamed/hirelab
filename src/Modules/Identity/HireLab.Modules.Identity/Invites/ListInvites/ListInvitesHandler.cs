using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.ListInvites;

internal sealed record ListInvitesQuery(StaffScope Scope);

/// <summary>Lists the invites in a scope, newest first, until 30 days after they expire.</summary>
internal sealed class ListInvitesHandler(IdentityModuleDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<ListInvitesQuery, Result<IReadOnlyList<InviteResponse>>>
{
    public static readonly TimeSpan ListedAfterExpiry = TimeSpan.FromDays(30);

    public async Task<Result<IReadOnlyList<InviteResponse>>> HandleAsync(ListInvitesQuery query, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var listedSince = now - ListedAfterExpiry;

        var invites = await query.Scope.Apply(dbContext.Invites.AsNoTracking())
            .Where(invite => invite.ExpiresAt > listedSince)
            .OrderByDescending(invite => invite.ExpiresAt)
            .ToListAsync(cancellationToken);

        var teamIds = invites.Where(invite => invite.TeamId is not null).Select(invite => invite.TeamId!.Value).Distinct().ToList();
        var teamNames = await dbContext.Teams.AsNoTracking()
            .Where(team => teamIds.Contains(team.Id))
            .ToDictionaryAsync(team => team.Id, team => team.Name, cancellationToken);

        return invites
            .Select(invite => InviteResponse.From(
                invite,
                invite.TeamId is { } teamId ? teamNames.GetValueOrDefault(teamId) : null,
                now))
            .ToList();
    }
}
