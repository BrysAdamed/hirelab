using HireLab.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Identity.Persistence.Configurations;

internal sealed class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public const string TokenHashIndex = "ix_invites_token_hash";
    public const string OneOpenInvitePerEmailIndex = "ix_invites_one_open_per_email";
    public const string TeamMatchesRoleConstraint = "ck_invites_team_matches_role";

    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.ToTable("invites", table => table.HasCheckConstraint(
            TeamMatchesRoleConstraint,
            "(role = 'Admin' AND team_id IS NULL AND new_team_name IS NULL) OR "
            + "(role = 'Manager' AND (team_id IS NULL) <> (new_team_name IS NULL)) OR "
            + "(role = 'Interviewer' AND team_id IS NOT NULL AND new_team_name IS NULL)"));

        builder.Property(invite => invite.Id).ValueGeneratedNever();
        builder.Property(invite => invite.Email).HasMaxLength(Invite.EmailMaxLength);
        builder.Property(invite => invite.NormalizedEmail).HasMaxLength(Invite.EmailMaxLength);
        builder.Property(invite => invite.DisplayName).HasMaxLength(User.DisplayNameMaxLength);
        builder.Property(invite => invite.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(invite => invite.NewTeamName).HasMaxLength(Team.NameMaxLength);
        builder.Property(invite => invite.TokenHash).HasMaxLength(64);
        builder.Property(invite => invite.Version).IsRowVersion();

        builder.HasIndex(invite => invite.TokenHash).HasDatabaseName(TokenHashIndex).IsUnique();

        builder.HasIndex(invite => invite.NormalizedEmail, OneOpenInvitePerEmailIndex)
            .HasDatabaseName(OneOpenInvitePerEmailIndex)
            .IsUnique()
            .HasFilter("used_at IS NULL AND revoked_at IS NULL");

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(invite => invite.TeamId)
            .HasConstraintName("fk_invites_teams_team_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invite => invite.TeamId).HasDatabaseName("ix_invites_team_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invite => invite.InvitedBy)
            .HasConstraintName("fk_invites_users_invited_by")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invite => invite.InvitedBy).HasDatabaseName("ix_invites_invited_by");
    }
}
