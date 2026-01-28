using MarketBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketBrief.Infrastructure.Data.Configurations;

public class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("news_articles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.GdeltUrl)
            .HasColumnName("gdelt_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.Snippet)
            .HasColumnName("snippet")
            .HasMaxLength(2000);

        builder.Property(e => e.SourceDomain)
            .HasColumnName("source_domain")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.PublishedAt)
            .HasColumnName("published_at")
            .IsRequired();

        builder.Property(e => e.QueryBucketName)
            .HasColumnName("query_bucket_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Tone)
            .HasColumnName("tone")
            .HasPrecision(10, 4);

        builder.Property(e => e.ClusterId)
            .HasColumnName("cluster_id");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => e.ClusterId);
        builder.HasIndex(e => e.PublishedAt);
        builder.HasIndex(e => e.QueryBucketName);
        builder.HasIndex(e => e.GdeltUrl).IsUnique();
    }
}
