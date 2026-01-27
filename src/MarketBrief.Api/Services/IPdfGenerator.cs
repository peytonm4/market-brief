using MarketBrief.Core.Entities;

namespace MarketBrief.Api.Services;

public interface IPdfGenerator
{
    byte[] GeneratePdf(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData);
    Task<string> GenerateAndSavePdfAsync(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData, CancellationToken cancellationToken = default);
}
