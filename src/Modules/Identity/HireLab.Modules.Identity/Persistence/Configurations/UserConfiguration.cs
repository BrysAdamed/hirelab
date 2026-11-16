using HireLab.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Identity.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string SingleAdminIndex = "ix_users_single_admin";
    public const string OneActiveManagerPerTeamIndex = "ix_users_one_active_manager_per_team";
    public const string TeamMatchesRoleConstraint = "ck_users_team_matches_role";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table => table.HasCheckConstraint(TeamMatchesRoleConstraint, "(role = 'Admin') = (team_id IS NULL)"));

        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.DisplayName).HasMaxLength(User.DisplayNameMaxLength);
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(user => user.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(user => user.TeamId)
            .HasConstraintName("fk_users_teams_team_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(user => user.TeamId).HasDatabaseName("ix_users_team_id");

        builder.HasIndex(user => user.Role, SingleAdminIndex)
            .HasDatabaseName(SingleAdminIndex)
            .IsUnique()
            .HasFilter("role = 'Admin'");

        builder.HasIndex(user => user.TeamId, OneActiveManagerPerTeamIndex)
            .HasDatabaseName(OneActiveManagerPerTeamIndex)
            .IsUnique()
            .HasFilter("role = 'Manager' AND is_active");
    }
}
