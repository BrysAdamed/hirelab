using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.TaskBank.Persistence;

internal sealed class TaskBankDbContext(DbContextOptions<TaskBankDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "taskbank";
}