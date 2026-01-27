using System.Text.Json;
using MarketBrief.Core.Entities;
using MarketBrief.Core.Enums;
using Microsoft.Extensions.Logging;

namespace MarketBrief.Infrastructure.External;

public class MarketDataApiClient : IMarketDataApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarketDataApiClient> _logger;

    // Yahoo Finance symbols mapped to our internal format
    private static readonly List<SymbolMapping> AllSymbols = new()
    {
        // Indices
        new("^GSPC", "SPX", "S&P 500", DataType.Index),
        new("^DJI", "DJI", "Dow Jones Industrial Average", DataType.Index),
        new("^IXIC", "IXIC", "NASDAQ Composite", DataType.Index),
        new("^RUT", "RUT", "Russell 2000", DataType.Index),

        // Sector ETFs
        new("XLK", "XLK", "Technology", DataType.Sector),
        new("XLF", "XLF", "Financials", DataType.Sector),
        new("XLV", "XLV", "Healthcare", DataType.Sector),
        new("XLE", "XLE", "Energy", DataType.Sector),
        new("XLI", "XLI", "Industrials", DataType.Sector),
        new("XLP", "XLP", "Consumer Staples", DataType.Sector),
        new("XLY", "XLY", "Consumer Discretionary", DataType.Sector),
        new("XLU", "XLU", "Utilities", DataType.Sector),

        // Commodities
        new("GC=F", "GC=F", "Gold", DataType.Commodity),
        new("SI=F", "SI=F", "Silver", DataType.Commodity),
        new("CL=F", "CL=F", "Crude Oil WTI", DataType.Commodity),
        new("NG=F", "NG=F", "Natural Gas", DataType.Commodity),

        // Currencies
        new("DX-Y.NYB", "DXY", "US Dollar Index", DataType.Currency),
        new("EURUSD=X", "EURUSD", "EUR/USD", DataType.Currency),
        new("JPY=X", "USDJPY", "USD/JPY", DataType.Currency),
        new("GBPUSD=X", "GBPUSD", "GBP/USD", DataType.Currency),
    };

    private record SymbolMapping(string YahooSymbol, string Symbol, string Name, DataType Type);

    public MarketDataApiClient(HttpClient httpClient, ILogger<MarketDataApiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _logger = logger;
    }

    public async Task<IEnumerable<MarketDataSnapshot>> FetchMarketDataAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var allData = new List<MarketDataSnapshot>();

        var tasks = new[]
        {
            FetchIndicesAsync(date, cancellationToken),
            FetchSectorDataAsync(date, cancellationToken),
            FetchCommoditiesAsync(date, cancellationToken),
            FetchCurrenciesAsync(date, cancellationToken)
        };

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            allData.AddRange(result);
        }

        return allData;
    }

    public async Task<IEnumerable<MarketDataSnapshot>> FetchIndicesAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var symbols = AllSymbols.Where(s => s.Type == DataType.Index).ToList();
        return await FetchQuotesAsync(symbols, date, cancellationToken);
    }

    public async Task<IEnumerable<MarketDataSnapshot>> FetchSectorDataAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var symbols = AllSymbols.Where(s => s.Type == DataType.Sector).ToList();
        return await FetchQuotesAsync(symbols, date, cancellationToken);
    }

    public async Task<IEnumerable<MarketDataSnapshot>> FetchCommoditiesAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var symbols = AllSymbols.Where(s => s.Type == DataType.Commodity).ToList();
        return await FetchQuotesAsync(symbols, date, cancellationToken);
    }

    public async Task<IEnumerable<MarketDataSnapshot>> FetchCurrenciesAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var symbols = AllSymbols.Where(s => s.Type == DataType.Currency).ToList();
        return await FetchQuotesAsync(symbols, date, cancellationToken);
    }

    private async Task<IEnumerable<MarketDataSnapshot>> FetchQuotesAsync(List<SymbolMapping> symbols, DateOnly date, CancellationToken cancellationToken)
    {
        var snapshots = new List<MarketDataSnapshot>();

        foreach (var mapping in symbols)
        {
            try
            {
                var snapshot = await FetchSingleQuoteAsync(mapping, date, cancellationToken);
                if (snapshot != null)
                {
                    snapshots.Add(snapshot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch {Symbol}, skipping", mapping.YahooSymbol);
            }
        }

        return snapshots;
    }

    private async Task<MarketDataSnapshot?> FetchSingleQuoteAsync(SymbolMapping mapping, DateOnly date, CancellationToken cancellationToken)
    {
        var encodedSymbol = Uri.EscapeDataString(mapping.YahooSymbol);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{encodedSymbol}?interval=1d&range=1d";

        _logger.LogInformation("Fetching {Symbol} from Yahoo Finance", mapping.YahooSymbol);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("chart", out var chart) &&
            chart.TryGetProperty("result", out var results) &&
            results.GetArrayLength() > 0)
        {
            var result = results[0];
            var meta = result.GetProperty("meta");

            var regularMarketPrice = meta.GetProperty("regularMarketPrice").GetDecimal();
            var previousClose = meta.TryGetProperty("chartPreviousClose", out var prevClose)
                ? prevClose.GetDecimal()
                : meta.TryGetProperty("previousClose", out var prev) ? prev.GetDecimal() : regularMarketPrice;

            decimal? open = null, high = null, low = null;
            long? volume = null;

            // Try to get OHLCV from indicators
            if (result.TryGetProperty("indicators", out var indicators) &&
                indicators.TryGetProperty("quote", out var quotes) &&
                quotes.GetArrayLength() > 0)
            {
                var quote = quotes[0];

                if (quote.TryGetProperty("open", out var openArr) && openArr.GetArrayLength() > 0)
                {
                    var openVal = openArr[0];
                    if (openVal.ValueKind != JsonValueKind.Null)
                        open = openVal.GetDecimal();
                }

                if (quote.TryGetProperty("high", out var highArr) && highArr.GetArrayLength() > 0)
                {
                    var highVal = highArr[0];
                    if (highVal.ValueKind != JsonValueKind.Null)
                        high = highVal.GetDecimal();
                }

                if (quote.TryGetProperty("low", out var lowArr) && lowArr.GetArrayLength() > 0)
                {
                    var lowVal = lowArr[0];
                    if (lowVal.ValueKind != JsonValueKind.Null)
                        low = lowVal.GetDecimal();
                }

                if (quote.TryGetProperty("volume", out var volArr) && volArr.GetArrayLength() > 0)
                {
                    var volVal = volArr[0];
                    if (volVal.ValueKind != JsonValueKind.Null)
                        volume = volVal.GetInt64();
                }
            }

            // Fallback to meta values if indicators missing
            if (!high.HasValue && meta.TryGetProperty("regularMarketDayHigh", out var metaHigh))
                high = metaHigh.GetDecimal();
            if (!low.HasValue && meta.TryGetProperty("regularMarketDayLow", out var metaLow))
                low = metaLow.GetDecimal();
            if (!volume.HasValue && meta.TryGetProperty("regularMarketVolume", out var metaVol))
                volume = metaVol.GetInt64();

            var changeAmount = regularMarketPrice - previousClose;
            var changePercent = previousClose != 0 ? (changeAmount / previousClose) * 100 : 0;

            var snapshot = new MarketDataSnapshot
            {
                Id = Guid.NewGuid(),
                SnapshotDate = date,
                DataType = mapping.Type,
                Symbol = mapping.Symbol,
                Name = mapping.Name,
                OpenPrice = open ?? regularMarketPrice,
                ClosePrice = regularMarketPrice,
                HighPrice = high,
                LowPrice = low,
                Volume = volume,
                ChangeAmount = changeAmount,
                ChangePercent = changePercent,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Fetched {Symbol}: {Price} ({Change:+0.00;-0.00}%)",
                mapping.Symbol, regularMarketPrice, changePercent);

            return snapshot;
        }

        _logger.LogWarning("No data returned for {Symbol}", mapping.YahooSymbol);
        return null;
    }
}
