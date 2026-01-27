using MarketBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketBrief.Infrastructure.Data.Configurations;

public class BriefSectionConfiguration : IEntityTypeConfiguration<BriefSection>
{
    public void Configure(EntityTypeBuilder<BriefSection> builder)
    {
        builder.ToTable("brief_sections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.BriefId)
            .HasColumnName("brief_id")
            .IsRequired();

        builder.Property(e => e.SectionType)
            .HasColumnName("section_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.ContentMarkdown)
            .HasColumnName("content_markdown");

        builder.Property(e => e.ContentJson)
            .HasColumnName("content_json")
            .HasColumnType("jsonb");

        builder.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.BriefId, e.DisplayOrder });
    }
}
