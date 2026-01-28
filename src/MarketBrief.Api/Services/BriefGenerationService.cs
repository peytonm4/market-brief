using System.Text.Json;
using MarketBrief.Api.Configuration;
using MarketBrief.Api.Services.News;
using MarketBrief.Core.Entities;
using MarketBrief.Core.Enums;
using MarketBrief.Infrastructure.Data;
using MarketBrief.Infrastructure.External;
using MarketBrief.Infrastructure.External.Gdelt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarketBrief.Api.Services;

public class BriefGenerationService : IBriefGenerationService
{
    private readonly MarketBriefDbContext _dbContext;
    private readonly IMarketDataApiClient _marketDataClient;
    private readonly IMarkdownGenerator _markdownGenerator;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IEmailNotificationService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BriefGenerationService> _logger;
    private readonly IGdeltNewsClient _gdeltClient;
    private readonly INewsImpactCalculator _impactCalculator;
    private readonly INewsDeduplicationService _deduplicationService;
    private readonly INewsRankingService _rankingService;
    private readonly GdeltOptions _gdeltOptions;

    private static GenerationLog? _currentGeneration;
    private static readonly object _lock = new();

    public BriefGenerationService(
        MarketBriefDbContext dbContext,
        IMarketDataApiClient marketDataClient,
        IMarkdownGenerator markdownGenerator,
        IPdfGenerator pdfGenerator,
        IEmailNotificationService emailService,
        IConfiguration configuration,
        ILogger<BriefGenerationService> logger,
        IGdeltNewsClient gdeltClient,
        INewsImpactCalculator impactCalculator,
        INewsDeduplicationService deduplicationService,
        INewsRankingService rankingService,
        IOptions<GdeltOptions> gdeltOptions)
    {
        _dbContext = dbContext;
        _marketDataClient = marketDataClient;
        _markdownGenerator = markdownGenerator;
        _pdfGenerator = pdfGenerator;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _gdeltClient = gdeltClient;
        _impactCalculator = impactCalculator;
        _deduplicationService = deduplicationService;
        _rankingService = rankingService;
        _gdeltOptions = gdeltOptions.Value;
    }

    public async Task<MarketBriefEntity> GenerateBriefAsync(DateOnly date, TriggerType triggerType, bool force = false, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Check if brief already exists
        var existingBrief = await _dbContext.MarketBriefs
            .FirstOrDefaultAsync(b => b.BriefDate == date, cancellationToken);

        if (existingBrief != null && existingBrief.Status == BriefStatus.Completed)
        {
            if (force)
            {
                _logger.LogInformation("Force regeneration requested for {Date}, deleting existing brief {BriefId}", date, existingBrief.Id);

                // Delete related sections
                var sections = await _dbContext.BriefSections
                    .Where(s => s.BriefId == existingBrief.Id)
                    .ToListAsync(cancellationToken);
                _dbContext.BriefSections.RemoveRange(sections);

                // Delete related news clusters
                var newsClusters = await _dbContext.NewsStoryClusters
                    .Where(c => c.BriefId == existingBrief.Id)
                    .ToListAsync(cancellationToken);
                _dbContext.NewsStoryClusters.RemoveRange(newsClusters);

                // Delete the brief itself
                _dbContext.MarketBriefs.Remove(existingBrief);
                await _dbContext.SaveChangesAsync(cancellationToken);

                existingBrief = null;
            }
            else
            {
                _logger.LogInformation("Brief for {Date} already exists with status Completed", date);
                return existingBrief;
            }
        }

        // Create generation log
        var generationLog = new GenerationLog
        {
            Id = Guid.NewGuid(),
            TriggerType = triggerType,
            Status = GenerationStatus.Running,
            StartedAt = startTime
        };

        lock (_lock)
        {
            _currentGeneration = generationLog;
        }

        _dbContext.GenerationLogs.Add(generationLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting brief generation for {Date}", date);

            // Create or update brief entity
            var brief = existingBrief ?? new MarketBriefEntity
            {
                Id = Guid.NewGuid(),
                BriefDate = date,
                CreatedAt = DateTime.UtcNow
            };

            brief.Title = $"Market Brief - {date:MMMM dd, yyyy}";
            brief.Status = BriefStatus.Generating;
            brief.GenerationStartedAt = startTime;
            brief.UpdatedAt = DateTime.UtcNow;

            if (existingBrief == null)
            {
                _dbContext.MarketBriefs.Add(brief);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            generationLog.BriefId = brief.Id;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Fetch market data
            _logger.LogInformation("Fetching market data for {Date}", date);
            var marketData = await _marketDataClient.FetchMarketDataAsync(date, cancellationToken);
            var marketDataList = marketData.ToList();

            // Save market data snapshots
            foreach (var snapshot in marketDataList)
            {
                var existing = await _dbContext.MarketDataSnapshots
                    .FirstOrDefaultAsync(s =>
                        s.SnapshotDate == date &&
                        s.DataType == snapshot.DataType &&
                        s.Symbol == snapshot.Symbol,
                        cancellationToken);

                if (existing == null)
                {
                    _dbContext.MarketDataSnapshots.Add(snapshot);
                }
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Fetch and process news (with graceful degradation)
            var rankedNews = await FetchAndProcessNewsAsync(brief.Id, cancellationToken);

            // Generate markdown content
            _logger.LogInformation("Generating markdown content");
            brief.Summary = GenerateSummary(marketDataList);
            brief.ContentMarkdown = _markdownGenerator.GenerateFullBrief(brief, marketDataList, rankedNews);
            brief.ContentJson = JsonSerializer.Serialize(CreateContentJson(brief, marketDataList, rankedNews));

            // Create sections
            await CreateSectionsAsync(brief, marketDataList, rankedNews, cancellationToken);

            // Generate PDF
            _logger.LogInformation("Generating PDF");
            var pdfPath = await _pdfGenerator.GenerateAndSavePdfAsync(brief, marketDataList, rankedNews, cancellationToken);
            brief.PdfStoragePath = pdfPath;
            brief.PdfGeneratedAt = DateTime.UtcNow;

            // Complete brief
            var endTime = DateTime.UtcNow;
            brief.Status = BriefStatus.Completed;
            brief.GenerationCompletedAt = endTime;
            brief.GenerationDurationMs = (int)(endTime - startTime).TotalMilliseconds;
            brief.UpdatedAt = endTime;

            // Update generation log
            generationLog.Status = GenerationStatus.Completed;
            generationLog.CompletedAt = endTime;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Brief generation completed for {Date} in {Duration}ms",
                date, brief.GenerationDurationMs);

            // Send email notification if configured
            if (_configuration.GetValue<bool>("Email:Enabled", false) && _emailService.IsConfigured)
            {
                _logger.LogInformation("Sending email notification for brief {BriefId}", brief.Id);
                var pdfBytes = _pdfGenerator.GeneratePdf(brief, marketDataList, rankedNews);
                await _emailService.SendBriefNotificationAsync(brief, pdfBytes, cancellationToken);
            }

            return brief;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate brief for {Date}", date);

            generationLog.Status = GenerationStatus.Failed;
            generationLog.CompletedAt = DateTime.UtcNow;
            generationLog.ErrorMessage = ex.Message;
            generationLog.ErrorStackTrace = ex.StackTrace;

            if (existingBrief != null)
            {
                existingBrief.Status = BriefStatus.Failed;
                existingBrief.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            throw;
        }
        finally
        {
            lock (_lock)
            {
                _currentGeneration = null;
            }
        }
    }

    public async Task<string> RegeneratePdfAsync(Guid briefId, CancellationToken cancellationToken = default)
    {
        var brief = await _dbContext.MarketBriefs
            .Include(b => b.Sections)
            .FirstOrDefaultAsync(b => b.Id == briefId, cancellationToken)
            ?? throw new InvalidOperationException($"Brief {briefId} not found");

        var marketData = await _dbContext.MarketDataSnapshots
            .Where(s => s.SnapshotDate == brief.BriefDate)
            .ToListAsync(cancellationToken);

        // Try to get existing news clusters for this brief
        var newsClusters = await _dbContext.NewsStoryClusters
            .Where(c => c.BriefId == briefId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        IEnumerable<RankedNewsStory>? rankedNews = null;
        if (newsClusters.Any())
        {
            rankedNews = newsClusters.Select(c => new RankedNewsStory(
                Headline: c.PrimaryHeadline,
                Url: string.Empty,
                SourceDomain: string.Empty,
                PublishedAt: c.CreatedAt,
                BucketName: c.QueryBucketName,
                BucketDisplayName: GetBucketDisplayName(c.QueryBucketName),
                ImpactScore: c.ImpactScore,
                PickupScore: c.PickupScore,
                RecencyScore: c.RecencyScore,
                RelevanceScore: c.RelevanceScore,
                FinalScore: c.FinalScore,
                ArticleCount: c.ArticleCount,
                TopSources: DeserializeSources(c.RepresentativeSourcesJson)
            ));
        }

        var pdfPath = await _pdfGenerator.GenerateAndSavePdfAsync(brief, marketData, rankedNews, cancellationToken);

        brief.PdfStoragePath = pdfPath;
        brief.PdfGeneratedAt = DateTime.UtcNow;
        brief.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return pdfPath;
    }

    private static string GetBucketDisplayName(string bucketName)
    {
        return bucketName switch
        {
            "macro_rates" => "Macro/Rates",
            "risk_volatility" => "Risk/Volatility",
            "oil_energy" => "Oil/Energy",
            "megacap_ai" => "Mega-cap/AI",
            "banks_credit" => "Banks/Credit",
            "crypto" => "Crypto",
            _ => bucketName
        };
    }

    private static IReadOnlyList<string> DeserializeSources(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public bool IsGenerationRunning()
    {
        lock (_lock)
        {
            return _currentGeneration != null;
        }
    }

    public GenerationLog? GetCurrentGenerationStatus()
    {
        lock (_lock)
        {
            return _currentGeneration;
        }
    }

    private string GenerateSummary(List<MarketDataSnapshot> marketData)
    {
        var sp500 = marketData.FirstOrDefault(d => d.Symbol == "SPX");
        if (sp500 == null)
        {
            return "Market data summary for today's trading session.";
        }

        var direction = sp500.ChangePercent >= 0 ? "higher" : "lower";
        var sectors = marketData.Where(d => d.DataType == DataType.Sector)
            .OrderByDescending(s => s.ChangePercent)
            .ToList();

        var topSector = sectors.FirstOrDefault()?.Name ?? "N/A";
        var bottomSector = sectors.LastOrDefault()?.Name ?? "N/A";

        return $"U.S. markets closed {direction} with the S&P 500 {(sp500.ChangePercent >= 0 ? "gaining" : "losing")} {Math.Abs(sp500.ChangePercent ?? 0):F2}%. " +
               $"{topSector} led sector performance while {bottomSector} lagged.";
    }

    private async Task CreateSectionsAsync(MarketBriefEntity brief, List<MarketDataSnapshot> marketData, IEnumerable<RankedNewsStory>? rankedNews, CancellationToken cancellationToken)
    {
        // Remove existing sections
        var existingSections = await _dbContext.BriefSections
            .Where(s => s.BriefId == brief.Id)
            .ToListAsync(cancellationToken);

        _dbContext.BriefSections.RemoveRange(existingSections);

        var displayOrder = 0;

        // Market Summary Section
        var indices = marketData.Where(d => d.DataType == DataType.Index).ToList();
        if (indices.Any())
        {
            _dbContext.BriefSections.Add(new BriefSection
            {
                Id = Guid.NewGuid(),
                BriefId = brief.Id,
                SectionType = SectionType.MarketSummary,
                Title = "Market Summary",
                ContentMarkdown = _markdownGenerator.GenerateMarketSummary(indices),
                DisplayOrder = displayOrder++
            });
        }

        // Sector Performance Section
        var sectors = marketData.Where(d => d.DataType == DataType.Sector).ToList();
        if (sectors.Any())
        {
            _dbContext.BriefSections.Add(new BriefSection
            {
                Id = Guid.NewGuid(),
                BriefId = brief.Id,
                SectionType = SectionType.SectorPerformance,
                Title = "Sector Performance",
                ContentMarkdown = _markdownGenerator.GenerateSectorPerformance(sectors),
                DisplayOrder = displayOrder++
            });
        }

        // Commodities Section
        var commodities = marketData.Where(d => d.DataType == DataType.Commodity).ToList();
        if (commodities.Any())
        {
            _dbContext.BriefSections.Add(new BriefSection
            {
                Id = Guid.NewGuid(),
                BriefId = brief.Id,
                SectionType = SectionType.Commodities,
                Title = "Commodities",
                ContentMarkdown = _markdownGenerator.GenerateCommoditiesSection(commodities),
                DisplayOrder = displayOrder++
            });
        }

        // Currencies Section
        var currencies = marketData.Where(d => d.DataType == DataType.Currency).ToList();
        if (currencies.Any())
        {
            _dbContext.BriefSections.Add(new BriefSection
            {
                Id = Guid.NewGuid(),
                BriefId = brief.Id,
                SectionType = SectionType.Currencies,
                Title = "Currencies",
                ContentMarkdown = _markdownGenerator.GenerateCurrenciesSection(currencies),
                DisplayOrder = displayOrder++
            });
        }

        // Market-Moving News Section
        var newsList = rankedNews?.ToList();
        if (newsList != null && newsList.Any())
        {
            _dbContext.BriefSections.Add(new BriefSection
            {
                Id = Guid.NewGuid(),
                BriefId = brief.Id,
                SectionType = SectionType.MarketMovingNews,
                Title = "Market-Moving News",
                ContentMarkdown = _markdownGenerator.GenerateNewsSection(newsList),
                DisplayOrder = displayOrder++
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private object CreateContentJson(MarketBriefEntity brief, List<MarketDataSnapshot> marketData, IEnumerable<RankedNewsStory>? rankedNews = null)
    {
        var newsList = rankedNews?.ToList();

        return new
        {
            title = brief.Title,
            date = brief.BriefDate.ToString("yyyy-MM-dd"),
            summary = brief.Summary,
            indices = marketData.Where(d => d.DataType == DataType.Index).Select(d => new
            {
                symbol = d.Symbol,
                name = d.Name,
                close = d.ClosePrice,
                change = d.ChangeAmount,
                changePercent = d.ChangePercent
            }),
            sectors = marketData.Where(d => d.DataType == DataType.Sector).Select(d => new
            {
                symbol = d.Symbol,
                name = d.Name,
                close = d.ClosePrice,
                changePercent = d.ChangePercent
            }),
            commodities = marketData.Where(d => d.DataType == DataType.Commodity).Select(d => new
            {
                symbol = d.Symbol,
                name = d.Name,
                price = d.ClosePrice,
                changePercent = d.ChangePercent
            }),
            currencies = marketData.Where(d => d.DataType == DataType.Currency).Select(d => new
            {
                symbol = d.Symbol,
                name = d.Name,
                rate = d.ClosePrice,
                changePercent = d.ChangePercent
            }),
            news = newsList?.Select(n => new
            {
                headline = n.Headline,
                url = n.Url,
                source = n.SourceDomain,
                publishedAt = n.PublishedAt,
                bucket = n.BucketName,
                bucketDisplayName = n.BucketDisplayName,
                articleCount = n.ArticleCount,
                score = n.FinalScore,
                topSources = n.TopSources
            })
        };
    }

    private async Task<IEnumerable<RankedNewsStory>?> FetchAndProcessNewsAsync(Guid briefId, CancellationToken cancellationToken)
    {
        if (!_gdeltOptions.Enabled)
        {
            _logger.LogInformation("GDELT news integration is disabled");
            return null;
        }

        try
        {
            _logger.LogInformation("Fetching news from GDELT for {BucketCount} query buckets", _gdeltOptions.Buckets.Count);

            var buckets = _gdeltOptions.Buckets.Select(b => new QueryBucket(b.Name, b.Query, b.DisplayName));

            var bucketResults = await _gdeltClient.FetchAllBucketsAsync(
                buckets,
                _gdeltOptions.MaxRecordsPerQuery,
                _gdeltOptions.DelayBetweenRequestsMs,
                cancellationToken);

            var bucketResultsList = bucketResults.ToList();

            // Calculate impact scores for each bucket
            var impactScores = new Dictionary<string, decimal>();
            var allClusters = new List<NewsCluster>();

            foreach (var result in bucketResultsList)
            {
                var impactScore = _impactCalculator.CalculateImpactScore(result.VolumeTimeline);
                impactScores[result.BucketName] = impactScore;

                _logger.LogDebug("Bucket '{Bucket}' impact score: {Score}", result.BucketName, impactScore);

                // Cluster articles within each bucket
                var clusters = _deduplicationService.ClusterArticles(
                    result.Articles,
                    result.BucketName,
                    _gdeltOptions.SimilarityThreshold);

                allClusters.AddRange(clusters);
            }

            _logger.LogInformation("Created {ClusterCount} total clusters from all buckets", allClusters.Count);

            // Rank all clusters
            var rankedStories = _rankingService.RankStories(
                allClusters,
                impactScores,
                _gdeltOptions.MaxStories,
                _gdeltOptions.MinArticlesPerCluster).ToList();

            _logger.LogInformation("Ranked {StoryCount} market-moving news stories", rankedStories.Count);

            // Save news clusters to database
            await SaveNewsClustersAsync(briefId, rankedStories, cancellationToken);

            return rankedStories;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch or process news from GDELT, continuing without news section");
            return null;
        }
    }

    private async Task SaveNewsClustersAsync(Guid briefId, IEnumerable<RankedNewsStory> rankedStories, CancellationToken cancellationToken)
    {
        var displayOrder = 0;

        foreach (var story in rankedStories)
        {
            var cluster = new NewsStoryCluster
            {
                Id = Guid.NewGuid(),
                BriefId = briefId,
                PrimaryHeadline = story.Headline,
                WhyItMatters = GetWhyItMatters(story.BucketName),
                QueryBucketName = story.BucketName,
                ImpactScore = story.ImpactScore,
                PickupScore = story.PickupScore,
                RecencyScore = story.RecencyScore,
                RelevanceScore = story.RelevanceScore,
                FinalScore = story.FinalScore,
                DisplayOrder = displayOrder++,
                ArticleCount = story.ArticleCount,
                RepresentativeSourcesJson = JsonSerializer.Serialize(story.TopSources),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.NewsStoryClusters.Add(cluster);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GetWhyItMatters(string bucketName)
    {
        return bucketName switch
        {
            "macro_rates" => "Central bank policy and inflation data directly impact equity valuations and bond yields.",
            "risk_volatility" => "Shifts in risk sentiment can trigger rapid portfolio rebalancing across asset classes.",
            "oil_energy" => "Energy prices affect corporate margins and consumer spending across the economy.",
            "megacap_ai" => "Large-cap tech movements often lead broader market direction due to index weighting.",
            "banks_credit" => "Banking sector health reflects credit conditions and economic outlook.",
            "crypto" => "Crypto market moves can signal shifts in risk appetite and institutional adoption trends.",
            _ => string.Empty
        };
    }
}
