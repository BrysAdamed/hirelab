using HireLab.Modules.Positions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Positions.Persistence.Configurations;

internal sealed class JobPositionConfiguration : IEntityTypeConfiguration<JobPosition>
{
    public void Configure(EntityTypeBuilder<JobPosition> builder)
    {
        builder.ToTable("job_positions");

        builder.Property(position => position.Id).ValueGeneratedNever();
        builder.Property(position => position.Title).HasMaxLength(JobPositionDetails.TitleMaxLength);
        builder.Property(position => position.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(position => position.Version).IsRowVersion();
        builder.Ignore(position => position.IsClosed);

        builder.HasIndex(position => new { position.TeamId, position.State }).HasDatabaseName("ix_job_positions_team_id_state");
    }
}
