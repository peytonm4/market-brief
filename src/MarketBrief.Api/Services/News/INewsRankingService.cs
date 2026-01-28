namespace MarketBrief.Api.Services.News;

public interface INewsRankingService
{
    IEnumerable<RankedNewsStory> RankStories(
        IEnumerable<NewsCluster> clusters,
        IDictionary<string, decimal> impactScores,
        int maxStories = 10,
        int minArticlesPerCluster = 2);
}

public record RankedNewsStory(
    string Headline,
    string Url,
    string SourceDomain,
    DateTime? PublishedAt,
    string BucketName,
    string BucketDisplayName,
    decimal ImpactScore,
    decimal PickupScore,
    decimal RecencyScore,
    decimal RelevanceScore,
    decimal FinalScore,
    int ArticleCount,
    IReadOnlyList<string> TopSources
);
