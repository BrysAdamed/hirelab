using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HireLab.SharedKernel.Persistence;

internal sealed class ModuleModelCustomizer<TContext>(ModelCustomizerDependencies dependencies)
    : RelationalModelCustomizer(dependencies)
    where TContext : DbContext, IModuleDbContext
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        modelBuilder.HasDefaultSchema(TContext.Schema);
        base.Customize(modelBuilder, context);
        AuditTimestamps.AddTo(modelBuilder);
        TeamQueryFilter.AddTo(modelBuilder, context);
    }
}
