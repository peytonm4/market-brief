using MarketBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketBrief.Infrastructure.Data.Configurations;

public class MarketDataSnapshotConfiguration : IEntityTypeConfiguration<MarketDataSnapshot>
{
    public void Configure(EntityTypeBuilder<MarketDataSnapshot> builder)
    {
        builder.ToTable("market_data_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.SnapshotDate)
            .HasColumnName("snapshot_date")
            .IsRequired();

        builder.Property(e => e.DataType)
            .HasColumnName("data_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.OpenPrice)
            .HasColumnName("open_price")
            .HasPrecision(18, 4);

        builder.Property(e => e.ClosePrice)
            .HasColumnName("close_price")
            .HasPrecision(18, 4);

        builder.Property(e => e.HighPrice)
            .HasColumnName("high_price")
            .HasPrecision(18, 4);

        builder.Property(e => e.LowPrice)
            .HasColumnName("low_price")
            .HasPrecision(18, 4);

        builder.Property(e => e.Volume)
            .HasColumnName("volume");

        builder.Property(e => e.ChangeAmount)
            .HasColumnName("change_amount")
            .HasPrecision(18, 4);

        builder.Property(e => e.ChangePercent)
            .HasColumnName("change_percent")
            .HasPrecision(10, 4);

        builder.Property(e => e.AdditionalData)
            .HasColumnName("additional_data")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.SnapshotDate, e.DataType, e.Symbol })
            .IsUnique();

        builder.HasIndex(e => e.SnapshotDate);
    }
}
