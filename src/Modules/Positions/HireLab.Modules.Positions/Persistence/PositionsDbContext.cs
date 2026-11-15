using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Persistence;

internal sealed class PositionsDbContext(DbContextOptions<PositionsDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "positions";
}