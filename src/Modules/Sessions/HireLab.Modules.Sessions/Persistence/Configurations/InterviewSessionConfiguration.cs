using HireLab.Modules.Sessions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Sessions.Persistence.Configurations;

internal sealed class InterviewSessionConfiguration : IEntityTypeConfiguration<InterviewSession>
{
    public const string NumberIndex = "ix_interview_sessions_candidate_id_number";

    public const string CandidateLinkTokenHashIndex = "ix_interview_sessions_candidate_link_token_hash";

    public void Configure(EntityTypeBuilder<InterviewSession> builder)
    {
        builder.ToTable("interview_sessions");

        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.Property(session => session.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(session => session.Version).IsRowVersion();
        builder.Ignore(session => session.Queue);
        builder.Ignore(session => session.IsPlanned);
        builder.Ignore(session => session.IsLive);
        builder.Ignore(session => session.NextTask);

        builder.HasMany(session => session.Tasks)
            .WithOne()
            .HasForeignKey(task => task.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(session => session.Tasks).HasField("_tasks");

        builder.OwnsOne(session => session.CandidateLink, link =>
        {
            link.Property(candidateLink => candidateLink.Id).HasColumnName("candidate_link_id");
            link.Property(candidateLink => candidateLink.TokenHash).HasColumnName("candidate_link_token_hash").HasMaxLength(64);
            link.Property(candidateLink => candidateLink.CreatedAt).HasColumnName("candidate_link_created_at");
            link.Property(candidateLink => candidateLink.CreatedBy).HasColumnName("candidate_link_created_by");
            link.HasIndex(candidateLink => candidateLink.TokenHash).IsUnique().HasDatabaseName(CandidateLinkTokenHashIndex);
        });

        builder.HasIndex(session => new { session.CandidateId, session.Number }).IsUnique().HasDatabaseName(NumberIndex);
        builder.HasIndex(session => new { session.TeamId, session.State }).HasDatabaseName("ix_interview_sessions_team_id_state");
    }
}
