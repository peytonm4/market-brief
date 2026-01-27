using MarketBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketBrief.Infrastructure.Data.Configurations;

public class MarketBriefConfiguration : IEntityTypeConfiguration<MarketBriefEntity>
{
    public void Configure(EntityTypeBuilder<MarketBriefEntity> builder)
    {
        builder.ToTable("market_briefs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.BriefDate)
            .HasColumnName("brief_date")
            .IsRequired();

        builder.HasIndex(e => e.BriefDate)
            .IsUnique();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Summary)
            .HasColumnName("summary")
            .HasMaxLength(2000);

        builder.Property(e => e.ContentMarkdown)
            .HasColumnName("content_markdown");

        builder.Property(e => e.ContentJson)
            .HasColumnName("content_json")
            .HasColumnType("jsonb");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.PdfStoragePath)
            .HasColumnName("pdf_storage_path")
            .HasMaxLength(1000);

        builder.Property(e => e.PdfGeneratedAt)
            .HasColumnName("pdf_generated_at");

        builder.Property(e => e.GenerationStartedAt)
            .HasColumnName("generation_started_at");

        builder.Property(e => e.GenerationCompletedAt)
            .HasColumnName("generation_completed_at");

        builder.Property(e => e.GenerationDurationMs)
            .HasColumnName("generation_duration_ms");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(e => e.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        builder.HasMany(e => e.Sections)
            .WithOne(s => s.Brief)
            .HasForeignKey(s => s.BriefId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.GenerationLogs)
            .WithOne(l => l.Brief)
            .HasForeignKey(l => l.BriefId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
