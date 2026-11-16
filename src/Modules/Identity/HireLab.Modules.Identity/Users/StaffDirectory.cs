using HireLab.Modules.Identity.Contracts;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Users;

internal sealed class StaffDirectory(IdentityModuleDbContext dbContext) : IStaffDirectory
{
    public async Task<IReadOnlyDictionary<UserId, string>> FindDisplayNamesAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        if (userIds.Count == 0)
        {
            return new Dictionary<UserId, string>();
        }

        return await dbContext.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
    }
}
