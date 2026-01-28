using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;

namespace MarketBrief.Infrastructure.External.Gdelt;

public class GdeltNewsClient : IGdeltNewsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GdeltNewsClient> _logger;
    private const string BaseUrl = "https://api.gdeltproject.org/api/v2/doc/doc";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GdeltNewsClient(HttpClient httpClient, ILogger<GdeltNewsClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public async Task<IEnumerable<GdeltArticleDto>> FetchArticlesAsync(
        string query,
        int maxRecords = 250,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var url = $"{BaseUrl}?query={encodedQuery}&mode=artlist&maxrecords={maxRecords}&format=json&sort=datedesc";

            _logger.LogInformation("Fetching GDELT articles for query: {Query}", query);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                _logger.LogWarning("Empty response from GDELT for query: {Query}", query);
                return Enumerable.Empty<GdeltArticleDto>();
            }

            var result = JsonSerializer.Deserialize<GdeltArticleResponse>(json, JsonOptions);
            var articles = result?.Articles ?? Enumerable.Empty<GdeltArticleDto>();

            _logger.LogInformation("Fetched {Count} articles for query: {Query}", articles.Count(), query);
            return articles;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error fetching GDELT articles for query: {Query}", query);
            return Enumerable.Empty<GdeltArticleDto>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parsing error for GDELT query: {Query}", query);
            return Enumerable.Empty<GdeltArticleDto>();
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogWarning("GDELT request timed out for query: {Query}", query);
            return Enumerable.Empty<GdeltArticleDto>();
        }
    }

    public async Task<IEnumerable<GdeltVolumeDataPoint>> FetchVolumeTimelineAsync(
        string query,
        string timespan = "24h",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var url = $"{BaseUrl}?query={encodedQuery}&mode=timelinevolraw&timespan={timespan}&format=json";

            _logger.LogDebug("Fetching GDELT volume timeline for query: {Query}", query);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                _logger.LogWarning("Empty timeline response from GDELT for query: {Query}", query);
                return Enumerable.Empty<GdeltVolumeDataPoint>();
            }

            var result = JsonSerializer.Deserialize<GdeltTimelineResponse>(json, JsonOptions);
            var dataPoints = result?.Timeline?.FirstOrDefault()?.Data ?? Enumerable.Empty<GdeltVolumeDataPoint>();

            _logger.LogDebug("Fetched {Count} volume data points for query: {Query}", dataPoints.Count(), query);
            return dataPoints;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error fetching GDELT timeline for query: {Query}", query);
            return Enumerable.Empty<GdeltVolumeDataPoint>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parsing error for GDELT timeline query: {Query}", query);
            return Enumerable.Empty<GdeltVolumeDataPoint>();
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogWarning("GDELT timeline request timed out for query: {Query}", query);
            return Enumerable.Empty<GdeltVolumeDataPoint>();
        }
    }

    public async Task<IEnumerable<BucketArticlesResult>> FetchAllBucketsAsync(
        IEnumerable<QueryBucket> buckets,
        int maxRecordsPerBucket = 250,
        int delayBetweenRequestsMs = 500,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BucketArticlesResult>();
        var bucketList = buckets.ToList();

        _logger.LogInformation("Fetching GDELT data for {Count} query buckets", bucketList.Count);

        foreach (var bucket in bucketList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var articles = await FetchArticlesAsync(bucket.Query, maxRecordsPerBucket, cancellationToken);

                if (delayBetweenRequestsMs > 0)
                    await Task.Delay(delayBetweenRequestsMs, cancellationToken);

                var timeline = await FetchVolumeTimelineAsync(bucket.Query, "24h", cancellationToken);

                results.Add(new BucketArticlesResult(bucket.Name, articles, timeline));

                _logger.LogInformation(
                    "Bucket '{BucketName}': {ArticleCount} articles, {TimelineCount} timeline points",
                    bucket.Name, articles.Count(), timeline.Count());

                if (delayBetweenRequestsMs > 0 && bucket != bucketList.Last())
                    await Task.Delay(delayBetweenRequestsMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch bucket '{BucketName}', skipping", bucket.Name);
                results.Add(new BucketArticlesResult(bucket.Name, Enumerable.Empty<GdeltArticleDto>(), Enumerable.Empty<GdeltVolumeDataPoint>()));
            }
        }

        return results;
    }
}
