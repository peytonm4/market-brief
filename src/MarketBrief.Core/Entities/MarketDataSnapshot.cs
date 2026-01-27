using MarketBrief.Core.Enums;

namespace MarketBrief.Core.Entities;

public class MarketDataSnapshot
{
    public Guid Id { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public DataType DataType { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? OpenPrice { get; set; }
    public decimal? ClosePrice { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public long? Volume { get; set; }
    public decimal? ChangeAmount { get; set; }
    public decimal? ChangePercent { get; set; }
    public string? AdditionalData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
