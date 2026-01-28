using MarketBrief.Infrastructure.External.Gdelt;

namespace MarketBrief.Api.Services.News;

public interface INewsImpactCalculator
{
    decimal CalculateImpactScore(IEnumerable<GdeltVolumeDataPoint> volumeTimeline);
}
