using MarketBrief.Core.Enums;

namespace MarketBrief.Core.Entities;

public class MarketBriefEntity
{
    public Guid Id { get; set; }
    public DateOnly BriefDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ContentMarkdown { get; set; }
    public string? ContentJson { get; set; }
    public BriefStatus Status { get; set; } = BriefStatus.Draft;
    public string? PdfStoragePath { get; set; }
    public DateTime? PdfGeneratedAt { get; set; }
    public DateTime? GenerationStartedAt { get; set; }
    public DateTime? GenerationCompletedAt { get; set; }
    public int? GenerationDurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public int Version { get; set; } = 1;

    // Navigation properties
    public ICollection<BriefSection> Sections { get; set; } = new List<BriefSection>();
    public ICollection<GenerationLog> GenerationLogs { get; set; } = new List<GenerationLog>();
}
