using HireLab.Modules.Positions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Positions.Persistence.Configurations;

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public const string SlotIndex = "ix_attachments_candidate_id_slot";
    public const string SlotRangeConstraint = "ck_attachments_slot_range";

    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments", table => table.HasCheckConstraint(
            SlotRangeConstraint,
            $"slot BETWEEN 1 AND {Attachment.MaxPerCandidate}"));

        builder.Property(attachment => attachment.Id).ValueGeneratedNever();
        builder.Property(attachment => attachment.FileName).HasMaxLength(Attachment.FileNameMaxLength);
        builder.Property(attachment => attachment.ContentType).HasMaxLength(100);
        builder.Ignore(attachment => attachment.BlobName);

        builder.HasOne<Candidate>().WithMany().HasForeignKey(attachment => attachment.CandidateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attachment => new { attachment.CandidateId, attachment.Slot })
            .IsUnique()
            .HasDatabaseName(SlotIndex);
    }
}
