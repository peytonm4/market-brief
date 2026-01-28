using Microsoft.Extensions.Logging;

namespace MarketBrief.Api.Services.News;

public class NewsRankingService : INewsRankingService
{
    private readonly ILogger<NewsRankingService> _logger;

    private const decimal ImpactWeight = 0.45m;
    private const decimal PickupWeight = 0.30m;
    private const decimal RecencyWeight = 0.15m;
    private const decimal RelevanceWeight = 0.10m;
    private const int MaxDomainsForPickup = 20;
    private const int RecencyDecayHours = 24;

    private static readonly Dictionary<string, string> BucketDisplayNames = new()
    {
        { "macro_rates", "Macro/Rates" },
        { "risk_volatility", "Risk/Volatility" },
        { "oil_energy", "Oil/Energy" },
        { "megacap_ai", "Mega-cap/AI" },
        { "banks_credit", "Banks/Credit" },
        { "crypto", "Crypto" }
    };

    private static readonly HashSet<string> MarketKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "market", "stock", "shares", "trading", "investor", "investors",
        "billion", "million", "percent", "rally", "surge", "plunge",
        "decline", "gains", "losses", "earnings", "revenue", "profit",
        "fed", "rate", "inflation", "gdp", "economy", "economic",
        "dow", "nasdaq", "s&p", "index", "etf", "bond", "yield",
        "oil", "gold", "crypto", "bitcoin", "bank", "hedge"
    };

    public NewsRankingService(ILogger<NewsRankingService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<RankedNewsStory> RankStories(
        IEnumerable<NewsCluster> clusters,
        IDictionary<string, decimal> impactScores,
        int maxStories = 10,
        int minArticlesPerCluster = 2)
    {
        var clusterList = clusters.ToList();

        var filteredClusters = clusterList
            .Where(c => c.Articles.Count >= minArticlesPerCluster)
            .ToList();

        _logger.LogInformation(
            "Ranking {FilteredCount} clusters (filtered from {TotalCount} with min {MinArticles} articles)",
            filteredClusters.Count, clusterList.Count, minArticlesPerCluster);

        var rankedStories = filteredClusters
            .Select(c => ScoreCluster(c, impactScores))
            .OrderByDescending(r => r.FinalScore)
            .Take(maxStories)
            .ToList();

        for (int i = 0; i < rankedStories.Count; i++)
        {
            _logger.LogDebug(
                "Rank {Rank}: {Headline} (Score: {Score:F3}, Bucket: {Bucket})",
                i + 1, TruncateHeadline(rankedStories[i].Headline), rankedStories[i].FinalScore, rankedStories[i].BucketName);
        }

        return rankedStories;
    }

    private RankedNewsStory ScoreCluster(NewsCluster cluster, IDictionary<string, decimal> impactScores)
    {
        var impactScore = impactScores.TryGetValue(cluster.BucketName, out var score) ? score : 0m;
        var pickupScore = CalculatePickupScore(cluster.DistinctDomains.Count);
        var recencyScore = CalculateRecencyScore(cluster.PrimaryPublishedAt);
        var relevanceScore = CalculateRelevanceScore(cluster.PrimaryHeadline);

        var finalScore =
            (ImpactWeight * impactScore) +
            (PickupWeight * pickupScore) +
            (RecencyWeight * recencyScore) +
            (RelevanceWeight * relevanceScore);

        var displayName = BucketDisplayNames.TryGetValue(cluster.BucketName, out var name)
            ? name
            : cluster.BucketName;

        var topSources = cluster.DistinctDomains
            .Take(5)
            .ToList()
            .AsReadOnly();

        return new RankedNewsStory(
            Headline: cluster.PrimaryHeadline,
            Url: cluster.PrimaryUrl,
            SourceDomain: cluster.PrimaryDomain,
            PublishedAt: cluster.PrimaryPublishedAt,
            BucketName: cluster.BucketName,
            BucketDisplayName: displayName,
            ImpactScore: impactScore,
            PickupScore: pickupScore,
            RecencyScore: recencyScore,
            RelevanceScore: relevanceScore,
            FinalScore: finalScore,
            ArticleCount: cluster.Articles.Count,
            TopSources: topSources
        );
    }

    private static decimal CalculatePickupScore(int distinctDomainCount)
    {
        return Math.Min(1m, (decimal)distinctDomainCount / MaxDomainsForPickup);
    }

    private static decimal CalculateRecencyScore(DateTime? publishedAt)
    {
        if (!publishedAt.HasValue)
            return 0.5m;

        var hoursAgo = (DateTime.UtcNow - publishedAt.Value).TotalHours;

        if (hoursAgo < 0)
            return 1m;

        if (hoursAgo >= RecencyDecayHours)
            return 0m;

        var decayFactor = Math.Exp(-hoursAgo / (RecencyDecayHours / 3.0));
        return (decimal)decayFactor;
    }

    private static decimal CalculateRelevanceScore(string headline)
    {
        if (string.IsNullOrWhiteSpace(headline))
            return 0m;

        var words = headline
            .ToLowerInvariant()
            .Split(new[] { ' ', '-', ',', '.', ':', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return 0m;

        var matchCount = words.Count(w => MarketKeywords.Contains(w));
        var density = (decimal)matchCount / words.Length;

        return Math.Min(1m, density * 3);
    }

    private static string TruncateHeadline(string headline)
    {
        const int maxLength = 60;
        if (headline.Length <= maxLength)
            return headline;
        return headline[..(maxLength - 3)] + "...";
    }
}
