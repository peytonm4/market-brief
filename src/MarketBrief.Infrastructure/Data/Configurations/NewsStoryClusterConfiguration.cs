using MarketBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketBrief.Infrastructure.Data.Configurations;

public class NewsStoryClusterConfiguration : IEntityTypeConfiguration<NewsStoryCluster>
{
    public void Configure(EntityTypeBuilder<NewsStoryCluster> builder)
    {
        builder.ToTable("news_story_clusters");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.BriefId)
            .HasColumnName("brief_id")
            .IsRequired();

        builder.Property(e => e.PrimaryHeadline)
            .HasColumnName("primary_headline")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.WhyItMatters)
            .HasColumnName("why_it_matters")
            .HasMaxLength(2000);

        builder.Property(e => e.QueryBucketName)
            .HasColumnName("query_bucket_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ImpactScore)
            .HasColumnName("impact_score")
            .HasPrecision(10, 4);

        builder.Property(e => e.PickupScore)
            .HasColumnName("pickup_score")
            .HasPrecision(10, 4);

        builder.Property(e => e.RecencyScore)
            .HasColumnName("recency_score")
            .HasPrecision(10, 4);

        builder.Property(e => e.RelevanceScore)
            .HasColumnName("relevance_score")
            .HasPrecision(10, 4);

        builder.Property(e => e.FinalScore)
            .HasColumnName("final_score")
            .HasPrecision(10, 4);

        builder.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        builder.Property(e => e.ArticleCount)
            .HasColumnName("article_count")
            .HasDefaultValue(0);

        builder.Property(e => e.RepresentativeSourcesJson)
            .HasColumnName("representative_sources_json")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => e.BriefId);
        builder.HasIndex(e => new { e.BriefId, e.DisplayOrder });

        builder.HasMany(e => e.Articles)
            .WithOne(a => a.Cluster)
            .HasForeignKey(a => a.ClusterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Brief)
            .WithMany()
            .HasForeignKey(e => e.BriefId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
