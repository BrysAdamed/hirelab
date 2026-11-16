using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Authentication;

internal sealed class StaffUserStore(IdentityModuleDbContext context) : UserOnlyStore<User, IdentityModuleDbContext, UserId>(context)
{
    public override UserId ConvertIdFromString(string? id) => UserId.TryParse(id, null, out var userId) ? userId : default;

    public override string? ConvertIdToString(UserId id) => id == default ? null : id.ToString();
}
