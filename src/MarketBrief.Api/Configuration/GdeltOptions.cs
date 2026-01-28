namespace MarketBrief.Api.Configuration;

public class GdeltOptions
{
    public const string SectionName = "Gdelt";

    public bool Enabled { get; set; } = true;
    public int MaxRecordsPerQuery { get; set; } = 250;
    public int DelayBetweenRequestsMs { get; set; } = 500;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxStories { get; set; } = 10;
    public int MinArticlesPerCluster { get; set; } = 2;
    public decimal SimilarityThreshold { get; set; } = 0.7m;

    public RankingWeights Weights { get; set; } = new();
    public List<QueryBucketConfig> Buckets { get; set; } = new();
}

public class RankingWeights
{
    public decimal Impact { get; set; } = 0.45m;
    public decimal Pickup { get; set; } = 0.30m;
    public decimal Recency { get; set; } = 0.15m;
    public decimal Relevance { get; set; } = 0.10m;
}

public class QueryBucketConfig
{
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
