using HireLab.Modules.TaskBank.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.TaskBank.Persistence.Configurations;

internal sealed class TaskVersionConfiguration : IEntityTypeConfiguration<TaskVersion>
{
    public const string NumberIndex = "ix_task_versions_task_id_number";
    public const string DraftIndex = "ix_task_versions_task_id_draft";

    public void Configure(EntityTypeBuilder<TaskVersion> builder)
    {
        builder.ToTable("task_versions", table => table.HasCheckConstraint(
            "ck_task_versions_published",
            "(state = 'Published') = (published_at IS NOT NULL)"));

        builder.Property(version => version.Id).ValueGeneratedNever();
        builder.Property(version => version.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(version => version.Title).HasMaxLength(TaskVersionDetails.TitleMaxLength);
        builder.Property(version => version.Summary).HasMaxLength(TaskVersionDetails.SummaryMaxLength);
        builder.Property(version => version.Level).HasConversion<string>().HasMaxLength(20);
        builder.Property(version => version.Tags).HasColumnType("text[]");
        builder.Property(version => version.Rubric).HasColumnType("jsonb");
        builder.Property(version => version.CandidateContent).HasColumnType("jsonb");
        builder.Property(version => version.StaffContent).HasColumnType("jsonb");
        builder.Property(version => version.ContentHash).HasMaxLength(64);
        builder.Ignore(version => version.IsPublished);

        builder.HasIndex(version => new { version.TaskId, version.Number }).IsUnique().HasDatabaseName(NumberIndex);
        builder.HasIndex(version => version.TaskId)
            .IsUnique()
            .HasFilter("state = 'Draft'")
            .HasDatabaseName(DraftIndex);
        builder.HasIndex(version => version.Tags).HasMethod("gin").HasDatabaseName("ix_task_versions_tags");
    }
}
