using System.Text.Json;
using MarketBrief.Core.Entities;
using MarketBrief.Core.Enums;
using MarketBrief.Infrastructure.Data;
using MarketBrief.Infrastructure.External;
using Microsoft.EntityFrameworkCore;

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

    private static GenerationLog? _currentGeneration;
    private static readonly object _lock = new();

    public BriefGenerationService(
        MarketBriefDbContext dbContext,
        IMarketDataApiClient marketDataClient,
        IMarkdownGenerator markdownGenerator,
        IPdfGenerator pdfGenerator,
        IEmailNotificationService emailService,
        IConfiguration configuration,
        ILogger<BriefGenerationService> logger)
    {
        _dbContext = dbContext;
        _marketDataClient = marketDataClient;
        _markdownGenerator = markdownGenerator;
        _pdfGenerator = pdfGenerator;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MarketBriefEntity> GenerateBriefAsync(DateOnly date, TriggerType triggerType, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Check if brief already exists
        var existingBrief = await _dbContext.MarketBriefs
            .FirstOrDefaultAsync(b => b.BriefDate == date, cancellationToken);

        if (existingBrief != null && existingBrief.Status == BriefStatus.Completed)
        {
            _logger.LogInformation("Brief for {Date} already exists with status Completed", date);
            return existingBrief;
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

            // Generate markdown content
            _logger.LogInformation("Generating markdown content");
            brief.Summary = GenerateSummary(marketDataList);
            brief.ContentMarkdown = _markdownGenerator.GenerateFullBrief(brief, marketDataList);
            brief.ContentJson = JsonSerializer.Serialize(CreateContentJson(brief, marketDataList));

            // Create sections
            await CreateSectionsAsync(brief, marketDataList, cancellationToken);

            // Generate PDF
            _logger.LogInformation("Generating PDF");
            var pdfPath = await _pdfGenerator.GenerateAndSavePdfAsync(brief, marketDataList, cancellationToken);
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
                var pdfBytes = _pdfGenerator.GeneratePdf(brief, marketDataList);
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

        var pdfPath = await _pdfGenerator.GenerateAndSavePdfAsync(brief, marketData, cancellationToken);

        brief.PdfStoragePath = pdfPath;
        brief.PdfGeneratedAt = DateTime.UtcNow;
        brief.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return pdfPath;
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

    private async Task CreateSectionsAsync(MarketBriefEntity brief, List<MarketDataSnapshot> marketData, CancellationToken cancellationToken)
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

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private object CreateContentJson(MarketBriefEntity brief, List<MarketDataSnapshot> marketData)
    {
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
            })
        };
    }
}
