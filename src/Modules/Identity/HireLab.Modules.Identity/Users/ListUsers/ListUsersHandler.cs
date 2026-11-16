using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Users.ListUsers;

internal sealed record ListUsersQuery(StaffScope Scope);

/// <summary>Lists the users in a scope, active and deactivated, by display name.</summary>
internal sealed class ListUsersHandler(IdentityModuleDbContext dbContext) : IQueryHandler<ListUsersQuery, Result<IReadOnlyList<UserResponse>>>
{
    public async Task<Result<IReadOnlyList<UserResponse>>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken) =>
        await query.Scope.Apply(dbContext.Users.AsNoTracking()).ToResponses(dbContext).ToListAsync(cancellationToken);
}
