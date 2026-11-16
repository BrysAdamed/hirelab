using HireLab.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Identity.Persistence.Configurations;

internal sealed class ResetLinkConfiguration : IEntityTypeConfiguration<ResetLink>
{
    public const string TokenHashIndex = "ix_reset_links_token_hash";
    public const string OneOpenLinkPerUserIndex = "ix_reset_links_one_open_per_user";

    public void Configure(EntityTypeBuilder<ResetLink> builder)
    {
        builder.ToTable("reset_links");

        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.Property(link => link.TokenHash).HasMaxLength(64);
        builder.Property(link => link.Version).IsRowVersion();

        builder.HasIndex(link => link.TokenHash).HasDatabaseName(TokenHashIndex).IsUnique();

        builder.HasIndex(link => link.UserId, OneOpenLinkPerUserIndex)
            .HasDatabaseName(OneOpenLinkPerUserIndex)
            .IsUnique()
            .HasFilter("used_at IS NULL AND revoked_at IS NULL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(link => link.UserId)
            .HasConstraintName("fk_reset_links_users_user_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(link => link.IssuedBy)
            .HasConstraintName("fk_reset_links_users_issued_by")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => link.IssuedBy).HasDatabaseName("ix_reset_links_issued_by");
    }
}
