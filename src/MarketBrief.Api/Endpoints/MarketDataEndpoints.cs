using MarketBrief.Core.Enums;
using MarketBrief.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketBrief.Api.Endpoints;

public static class MarketDataEndpoints
{
    public static IEndpointRouteBuilder MapMarketDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market-data")
            .WithTags("MarketData");

        group.MapGet("/", GetMarketData)
            .WithName("GetMarketData")
            .WithDescription("Get market data for a specific date");

        group.MapGet("/latest", GetLatestMarketData)
            .WithName("GetLatestMarketData")
            .WithDescription("Get the most recent market data");

        return app;
    }

    private static async Task<IResult> GetMarketData(
        MarketBriefDbContext dbContext,
        DateOnly? date = null,
        string? dataType = null,
        CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = dbContext.MarketDataSnapshots
            .AsNoTracking()
            .Where(s => s.SnapshotDate == targetDate);

        if (!string.IsNullOrEmpty(dataType) && Enum.TryParse<DataType>(dataType, true, out var type))
        {
            query = query.Where(s => s.DataType == type);
        }

        var data = await query
            .OrderBy(s => s.DataType)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.SnapshotDate,
                DataType = s.DataType.ToString(),
                s.Symbol,
                s.Name,
                s.OpenPrice,
                s.ClosePrice,
                s.HighPrice,
                s.LowPrice,
                s.Volume,
                s.ChangeAmount,
                s.ChangePercent
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            date = targetDate,
            count = data.Count,
            data
        });
    }

    private static async Task<IResult> GetLatestMarketData(
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var latestDate = await dbContext.MarketDataSnapshots
            .AsNoTracking()
            .MaxAsync(s => (DateOnly?)s.SnapshotDate, cancellationToken);

        if (latestDate == null)
        {
            return Results.NotFound(new { message = "No market data available" });
        }

        var data = await dbContext.MarketDataSnapshots
            .AsNoTracking()
            .Where(s => s.SnapshotDate == latestDate.Value)
            .OrderBy(s => s.DataType)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.SnapshotDate,
                DataType = s.DataType.ToString(),
                s.Symbol,
                s.Name,
                s.OpenPrice,
                s.ClosePrice,
                s.HighPrice,
                s.LowPrice,
                s.Volume,
                s.ChangeAmount,
                s.ChangePercent
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            date = latestDate.Value,
            count = data.Count,
            data
        });
    }
}
