namespace MarketBrief.Core.Entities;

public class NewsArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GdeltUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public string SourceDomain { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string QueryBucketName { get; set; } = string.Empty;
    public decimal? Tone { get; set; }
    public Guid? ClusterId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public NewsStoryCluster? Cluster { get; set; }
}
