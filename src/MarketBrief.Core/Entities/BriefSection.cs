using MarketBrief.Core.Enums;

namespace MarketBrief.Core.Entities;

public class BriefSection
{
    public Guid Id { get; set; }
    public Guid BriefId { get; set; }
    public SectionType SectionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContentMarkdown { get; set; }
    public string? ContentJson { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public MarketBriefEntity Brief { get; set; } = null!;
}
