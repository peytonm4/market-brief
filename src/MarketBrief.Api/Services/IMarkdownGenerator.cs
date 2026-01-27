using MarketBrief.Core.Entities;

namespace MarketBrief.Api.Services;

public interface IMarkdownGenerator
{
    string GenerateFullBrief(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData);
    string GenerateMarketSummary(IEnumerable<MarketDataSnapshot> indices);
    string GenerateKeyMetrics(IEnumerable<MarketDataSnapshot> data);
    string GenerateSectorPerformance(IEnumerable<MarketDataSnapshot> sectors);
    string GenerateCommoditiesSection(IEnumerable<MarketDataSnapshot> commodities);
    string GenerateCurrenciesSection(IEnumerable<MarketDataSnapshot> currencies);
}
