using MarketBrief.Infrastructure.External.Gdelt;

namespace MarketBrief.Api.Services.News;

public interface INewsDeduplicationService
{
    IEnumerable<NewsCluster> ClusterArticles(
        IEnumerable<GdeltArticleDto> articles,
        string bucketName,
        decimal similarityThreshold = 0.7m);
}

public record NewsCluster(
    string PrimaryHeadline,
    string PrimaryUrl,
    string PrimaryDomain,
    DateTime? PrimaryPublishedAt,
    decimal? PrimaryTone,
    string BucketName,
    IReadOnlyList<GdeltArticleDto> Articles,
    IReadOnlyList<string> DistinctDomains
);
