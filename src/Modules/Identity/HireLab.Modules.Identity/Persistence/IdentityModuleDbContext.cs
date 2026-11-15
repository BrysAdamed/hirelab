using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Persistence;

internal sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "identity";
}