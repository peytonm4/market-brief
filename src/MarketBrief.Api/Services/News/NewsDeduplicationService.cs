using System.Globalization;
using System.Text.RegularExpressions;
using MarketBrief.Infrastructure.External.Gdelt;
using Microsoft.Extensions.Logging;

namespace MarketBrief.Api.Services.News;

public partial class NewsDeduplicationService : INewsDeduplicationService
{
    private readonly ILogger<NewsDeduplicationService> _logger;

    private static readonly HashSet<string> PreferredSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "reuters.com",
        "bloomberg.com",
        "wsj.com",
        "cnbc.com",
        "ft.com",
        "marketwatch.com",
        "apnews.com",
        "nytimes.com"
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "as", "is", "was", "are", "were", "been",
        "be", "have", "has", "had", "do", "does", "did", "will", "would",
        "could", "should", "may", "might", "must", "shall", "can", "need",
        "this", "that", "these", "those", "it", "its", "they", "their",
        "he", "she", "him", "her", "his", "hers", "we", "us", "our", "you", "your"
    };

    public NewsDeduplicationService(ILogger<NewsDeduplicationService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<NewsCluster> ClusterArticles(
        IEnumerable<GdeltArticleDto> articles,
        string bucketName,
        decimal similarityThreshold = 0.7m)
    {
        var articleList = articles.ToList();

        if (articleList.Count == 0)
        {
            _logger.LogDebug("No articles to cluster for bucket: {Bucket}", bucketName);
            return Enumerable.Empty<NewsCluster>();
        }

        var tokenizedArticles = articleList
            .Select(a => new
            {
                Article = a,
                Tokens = TokenizeHeadline(a.Title)
            })
            .Where(x => x.Tokens.Count > 0)
            .ToList();

        if (tokenizedArticles.Count == 0)
        {
            _logger.LogWarning("No valid tokenized articles for bucket: {Bucket}", bucketName);
            return Enumerable.Empty<NewsCluster>();
        }

        var clusters = new List<List<GdeltArticleDto>>();
        var assigned = new HashSet<int>();

        for (int i = 0; i < tokenizedArticles.Count; i++)
        {
            if (assigned.Contains(i))
                continue;

            var cluster = new List<GdeltArticleDto> { tokenizedArticles[i].Article };
            assigned.Add(i);

            for (int j = i + 1; j < tokenizedArticles.Count; j++)
            {
                if (assigned.Contains(j))
                    continue;

                var similarity = CalculateJaccardSimilarity(
                    tokenizedArticles[i].Tokens,
                    tokenizedArticles[j].Tokens);

                if (similarity >= similarityThreshold)
                {
                    cluster.Add(tokenizedArticles[j].Article);
                    assigned.Add(j);
                }
            }

            clusters.Add(cluster);
        }

        _logger.LogInformation(
            "Clustered {ArticleCount} articles into {ClusterCount} clusters for bucket: {Bucket}",
            articleList.Count, clusters.Count, bucketName);

        return clusters.Select(c => CreateCluster(c, bucketName));
    }

    private NewsCluster CreateCluster(List<GdeltArticleDto> articles, string bucketName)
    {
        var primaryArticle = SelectPrimaryArticle(articles);
        var distinctDomains = articles
            .Select(a => a.Domain ?? "unknown")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new NewsCluster(
            PrimaryHeadline: primaryArticle.Title,
            PrimaryUrl: primaryArticle.Url,
            PrimaryDomain: primaryArticle.Domain ?? "unknown",
            PrimaryPublishedAt: ParseGdeltDate(primaryArticle.Seendate),
            PrimaryTone: primaryArticle.Tone.HasValue ? (decimal)primaryArticle.Tone.Value : null,
            BucketName: bucketName,
            Articles: articles.AsReadOnly(),
            DistinctDomains: distinctDomains.AsReadOnly()
        );
    }

    private GdeltArticleDto SelectPrimaryArticle(List<GdeltArticleDto> articles)
    {
        var preferred = articles.FirstOrDefault(a =>
            a.Domain != null && PreferredSources.Contains(a.Domain));

        if (preferred != null)
            return preferred;

        return articles
            .OrderByDescending(a => ParseGdeltDate(a.Seendate) ?? DateTime.MinValue)
            .First();
    }

    private HashSet<string> TokenizeHeadline(string? headline)
    {
        if (string.IsNullOrWhiteSpace(headline))
            return new HashSet<string>();

        var words = WordTokenizer().Matches(headline.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .ToHashSet();

        return words;
    }

    private static decimal CalculateJaccardSimilarity(HashSet<string> set1, HashSet<string> set2)
    {
        if (set1.Count == 0 || set2.Count == 0)
            return 0m;

        var intersection = set1.Intersect(set2).Count();
        var union = set1.Union(set2).Count();

        return union == 0 ? 0m : (decimal)intersection / union;
    }

    private static DateTime? ParseGdeltDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParseExact(dateStr, "yyyyMMddHHmmss", null, DateTimeStyles.AssumeUniversal, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        if (DateTime.TryParse(dateStr, out dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        return null;
    }

    [GeneratedRegex(@"\b[a-z]+\b")]
    private static partial Regex WordTokenizer();
}
