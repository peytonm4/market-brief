namespace MarketBrief.Core.Entities;

public class NewsStoryCluster
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BriefId { get; set; }
    public string PrimaryHeadline { get; set; } = string.Empty;
    public string? WhyItMatters { get; set; }
    public string QueryBucketName { get; set; } = string.Empty;
    public decimal ImpactScore { get; set; }
    public decimal PickupScore { get; set; }
    public decimal RecencyScore { get; set; }
    public decimal RelevanceScore { get; set; }
    public decimal FinalScore { get; set; }
    public int DisplayOrder { get; set; }
    public int ArticleCount { get; set; }
    public string? RepresentativeSourcesJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public MarketBriefEntity Brief { get; set; } = null!;
    public ICollection<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
}
