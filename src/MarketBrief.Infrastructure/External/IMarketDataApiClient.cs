using MarketBrief.Core.Entities;

namespace MarketBrief.Infrastructure.External;

public interface IMarketDataApiClient
{
    Task<IEnumerable<MarketDataSnapshot>> FetchMarketDataAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketDataSnapshot>> FetchIndicesAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketDataSnapshot>> FetchSectorDataAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketDataSnapshot>> FetchCommoditiesAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketDataSnapshot>> FetchCurrenciesAsync(DateOnly date, CancellationToken cancellationToken = default);
}
