using MarketBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketBrief.Infrastructure.Data.Configurations;

public class GenerationLogConfiguration : IEntityTypeConfiguration<GenerationLog>
{
    public void Configure(EntityTypeBuilder<GenerationLog> builder)
    {
        builder.ToTable("generation_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.BriefId)
            .HasColumnName("brief_id");

        builder.Property(e => e.JobId)
            .HasColumnName("job_id")
            .HasMaxLength(100);

        builder.Property(e => e.TriggerType)
            .HasColumnName("trigger_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at")
            .HasDefaultValueSql("now()");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.ErrorStackTrace)
            .HasColumnName("error_stack_trace");

        builder.Property(e => e.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.HasIndex(e => e.BriefId);
        builder.HasIndex(e => e.StartedAt);
        builder.HasIndex(e => e.Status);
    }
}
