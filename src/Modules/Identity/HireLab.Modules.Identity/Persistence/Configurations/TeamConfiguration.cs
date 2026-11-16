using HireLab.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Identity.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.Property(team => team.Id).ValueGeneratedNever();
        builder.Property(team => team.Name).HasMaxLength(Team.NameMaxLength);
        builder.Property(team => team.Version).IsRowVersion();
    }
}
