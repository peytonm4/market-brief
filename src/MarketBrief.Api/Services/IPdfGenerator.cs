using MarketBrief.Api.Services.News;
using MarketBrief.Core.Entities;

namespace MarketBrief.Api.Services;

public interface IPdfGenerator
{
    byte[] GeneratePdf(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData, IEnumerable<RankedNewsStory>? rankedNews = null);
    Task<string> GenerateAndSavePdfAsync(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData, IEnumerable<RankedNewsStory>? rankedNews = null, CancellationToken cancellationToken = default);
}
