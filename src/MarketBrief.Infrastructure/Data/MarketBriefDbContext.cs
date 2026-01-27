using MarketBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketBrief.Infrastructure.Data;

public class MarketBriefDbContext : DbContext
{
    public MarketBriefDbContext(DbContextOptions<MarketBriefDbContext> options)
        : base(options)
    {
    }

    public DbSet<MarketBriefEntity> MarketBriefs => Set<MarketBriefEntity>();
    public DbSet<BriefSection> BriefSections => Set<BriefSection>();
    public DbSet<MarketDataSnapshot> MarketDataSnapshots => Set<MarketDataSnapshot>();
    public DbSet<GenerationLog> GenerationLogs => Set<GenerationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketBriefDbContext).Assembly);
    }
}
