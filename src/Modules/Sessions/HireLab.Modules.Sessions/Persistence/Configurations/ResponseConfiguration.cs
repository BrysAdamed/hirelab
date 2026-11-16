using HireLab.Modules.Sessions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Sessions.Persistence.Configurations;

internal sealed class ResponseConfiguration : IEntityTypeConfiguration<Response>
{
    public const string PrimaryKey = "pk_responses";

    public void Configure(EntityTypeBuilder<Response> builder)
    {
        builder.ToTable("responses");

        builder.HasKey(response => response.SessionTaskId).HasName(PrimaryKey);
        builder.Property(response => response.SessionTaskId).ValueGeneratedNever();
        builder.Property(response => response.Content).HasColumnType("jsonb");
        builder.Property(response => response.Revision).IsConcurrencyToken();

        builder.HasOne<SessionTask>()
            .WithOne()
            .HasForeignKey<Response>(response => response.SessionTaskId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(response => response.SessionId).HasDatabaseName("ix_responses_session_id");
    }
}
