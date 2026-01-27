using MarketBrief.Core.Entities;
using MarketBrief.Core.Enums;

namespace MarketBrief.Api.Services;

public interface IBriefGenerationService
{
    Task<MarketBriefEntity> GenerateBriefAsync(DateOnly date, TriggerType triggerType, CancellationToken cancellationToken = default);
    Task<string> RegeneratePdfAsync(Guid briefId, CancellationToken cancellationToken = default);
    bool IsGenerationRunning();
    GenerationLog? GetCurrentGenerationStatus();
}
