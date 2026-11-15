using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Evaluation.Persistence;

internal sealed class EvaluationDbContext(DbContextOptions<EvaluationDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "evaluation";
}