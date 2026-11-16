using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Persistence;

internal static class TeamQueries
{
    public static Task<bool> HasActiveManagerAsync(this IdentityModuleDbContext dbContext, TeamId teamId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(
            user => user.TeamId == teamId && user.Role == StaffRole.Manager && user.IsActive,
            cancellationToken);
}
