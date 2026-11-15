using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Persistence;

internal sealed class SessionsDbContext(DbContextOptions<SessionsDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "sessions";
}