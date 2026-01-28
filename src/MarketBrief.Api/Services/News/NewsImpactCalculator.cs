using MarketBrief.Infrastructure.External.Gdelt;
using Microsoft.Extensions.Logging;

namespace MarketBrief.Api.Services.News;

public class NewsImpactCalculator : INewsImpactCalculator
{
    private readonly ILogger<NewsImpactCalculator> _logger;
    private const int RecentHours = 3;
    private const int BaselineHours = 7;

    public NewsImpactCalculator(ILogger<NewsImpactCalculator> logger)
    {
        _logger = logger;
    }

    public decimal CalculateImpactScore(IEnumerable<GdeltVolumeDataPoint> volumeTimeline)
    {
        var dataPoints = volumeTimeline.ToList();

        if (dataPoints.Count == 0)
        {
            _logger.LogDebug("No volume data points, returning 0 impact score");
            return 0m;
        }

        var sortedPoints = dataPoints
            .Select(dp => new
            {
                Timestamp = ParseGdeltDate(dp.Date),
                dp.Value
            })
            .Where(x => x.Timestamp.HasValue)
            .OrderByDescending(x => x.Timestamp!.Value)
            .ToList();

        if (sortedPoints.Count == 0)
        {
            _logger.LogDebug("No valid timestamps in volume data, returning 0 impact score");
            return 0m;
        }

        var now = DateTime.UtcNow;
        var recentCutoff = now.AddHours(-RecentHours);

        var recentVolume = sortedPoints
            .Where(x => x.Timestamp >= recentCutoff)
            .Sum(x => x.Value);

        var totalVolume = sortedPoints.Sum(x => x.Value);
        var olderVolume = totalVolume - recentVolume;

        if (olderVolume <= 0)
        {
            _logger.LogDebug("No older volume for baseline, using total volume ratio");
            return Math.Min(1m, (decimal)recentVolume / 100m);
        }

        var baselineVolume = (decimal)olderVolume / BaselineHours * RecentHours;

        if (baselineVolume <= 0)
        {
            return Math.Min(1m, (decimal)recentVolume / 100m);
        }

        var spikeRatio = (decimal)recentVolume / baselineVolume;
        var impactScore = Math.Min(1m, Math.Max(0m, (spikeRatio - 1m) / 2m));

        _logger.LogDebug(
            "Volume spike calculation: recent={Recent}, baseline={Baseline}, ratio={Ratio}, score={Score}",
            recentVolume, baselineVolume, spikeRatio, impactScore);

        return impactScore;
    }

    private static DateTime? ParseGdeltDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParseExact(dateStr, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
            return dt;

        if (DateTime.TryParse(dateStr, out dt))
            return dt;

        return null;
    }
}
