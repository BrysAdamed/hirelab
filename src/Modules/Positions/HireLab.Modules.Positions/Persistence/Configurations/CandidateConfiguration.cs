using HireLab.Modules.Positions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Positions.Persistence.Configurations;

internal sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public const string NumberIndex = "ix_candidates_job_position_id_number";

    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates");

        builder.Property(candidate => candidate.Id).ValueGeneratedNever();
        builder.Property(candidate => candidate.FullName).HasMaxLength(CandidateDetails.FullNameMaxLength);
        builder.Property(candidate => candidate.Email).HasMaxLength(CandidateDetails.EmailMaxLength);
        builder.Property(candidate => candidate.Phone).HasMaxLength(CandidateDetails.PhoneMaxLength);
        builder.Property(candidate => candidate.Version).IsRowVersion();
        builder.Ignore(candidate => candidate.IsAnonymised);
        builder.Ignore(candidate => candidate.DisplayName);

        builder.HasOne<JobPosition>().WithMany().HasForeignKey(candidate => candidate.JobPositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(candidate => new { candidate.JobPositionId, candidate.Number })
            .IsUnique()
            .HasDatabaseName(NumberIndex);
    }
}
