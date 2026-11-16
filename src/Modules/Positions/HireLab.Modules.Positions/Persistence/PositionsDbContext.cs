using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HireLab.Modules.Positions.Persistence;

internal sealed class PositionsDbContext(DbContextOptions<PositionsDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "positions";

    public DbSet<JobPosition> JobPositions => Set<JobPosition>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddSharedKernelIdConversions();
        configurationBuilder.Properties<JobPositionId>().HaveConversion<JobPositionIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PositionsDbContext).Assembly);

    private sealed class JobPositionIdConverter() : ValueConverter<JobPositionId, Guid>(id => id.Value, value => new JobPositionId(value));
}
