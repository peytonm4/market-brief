using MarketBrief.Core.Enums;

namespace MarketBrief.Core.Entities;

public class GenerationLog
{
    public Guid Id { get; set; }
    public Guid? BriefId { get; set; }
    public string? JobId { get; set; }
    public TriggerType TriggerType { get; set; }
    public GenerationStatus Status { get; set; } = GenerationStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public string? Metadata { get; set; }

    // Navigation property
    public MarketBriefEntity? Brief { get; set; }
}
