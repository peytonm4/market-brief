namespace MarketBrief.Infrastructure.External.Gdelt;

public interface IGdeltNewsClient
{
    Task<IEnumerable<GdeltArticleDto>> FetchArticlesAsync(
        string query,
        int maxRecords = 250,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<GdeltVolumeDataPoint>> FetchVolumeTimelineAsync(
        string query,
        string timespan = "24h",
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BucketArticlesResult>> FetchAllBucketsAsync(
        IEnumerable<QueryBucket> buckets,
        int maxRecordsPerBucket = 250,
        int delayBetweenRequestsMs = 500,
        CancellationToken cancellationToken = default);
}
