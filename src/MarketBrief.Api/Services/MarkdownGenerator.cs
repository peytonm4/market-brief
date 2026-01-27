using System.Text;
using MarketBrief.Core.Entities;
using MarketBrief.Core.Enums;

namespace MarketBrief.Api.Services;

public class MarkdownGenerator : IMarkdownGenerator
{
    public string GenerateFullBrief(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData)
    {
        var sb = new StringBuilder();
        var dataList = marketData.ToList();

        sb.AppendLine($"# {brief.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Date:** {brief.BriefDate:MMMM dd, yyyy}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(brief.Summary))
        {
            sb.AppendLine("## Executive Summary");
            sb.AppendLine();
            sb.AppendLine(brief.Summary);
            sb.AppendLine();
        }

        // Market Summary
        var indices = dataList.Where(d => d.DataType == DataType.Index).ToList();
        if (indices.Any())
        {
            sb.AppendLine(GenerateMarketSummary(indices));
        }

        // Key Metrics
        sb.AppendLine(GenerateKeyMetrics(dataList));

        // Sector Performance
        var sectors = dataList.Where(d => d.DataType == DataType.Sector).ToList();
        if (sectors.Any())
        {
            sb.AppendLine(GenerateSectorPerformance(sectors));
        }

        // Commodities
        var commodities = dataList.Where(d => d.DataType == DataType.Commodity).ToList();
        if (commodities.Any())
        {
            sb.AppendLine(GenerateCommoditiesSection(commodities));
        }

        // Currencies
        var currencies = dataList.Where(d => d.DataType == DataType.Currency).ToList();
        if (currencies.Any())
        {
            sb.AppendLine(GenerateCurrenciesSection(currencies));
        }

        // Market Outlook
        sb.AppendLine("## Market Outlook");
        sb.AppendLine();
        sb.AppendLine(GenerateMarketOutlook(dataList));
        sb.AppendLine();

        // Footer
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }

    public string GenerateMarketSummary(IEnumerable<MarketDataSnapshot> indices)
    {
        var sb = new StringBuilder();
        var indexList = indices.ToList();

        sb.AppendLine("## Market Summary");
        sb.AppendLine();

        var sp500 = indexList.FirstOrDefault(i => i.Symbol == "SPX");
        if (sp500 != null)
        {
            var direction = sp500.ChangePercent >= 0 ? "gained" : "lost";
            var emoji = sp500.ChangePercent >= 0 ? "up" : "down";
            sb.AppendLine($"The S&P 500 {direction} **{Math.Abs(sp500.ChangePercent ?? 0):F2}%** to close at **{sp500.ClosePrice:N2}**.");
            sb.AppendLine();
        }

        sb.AppendLine("| Index | Close | Change | % Change |");
        sb.AppendLine("|-------|------:|-------:|---------:|");

        foreach (var index in indexList)
        {
            var changeSign = index.ChangeAmount >= 0 ? "+" : "";
            var pctSign = index.ChangePercent >= 0 ? "+" : "";
            sb.AppendLine($"| {index.Name} | {index.ClosePrice:N2} | {changeSign}{index.ChangeAmount:N2} | {pctSign}{index.ChangePercent:F2}% |");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    public string GenerateKeyMetrics(IEnumerable<MarketDataSnapshot> data)
    {
        var sb = new StringBuilder();
        var dataList = data.ToList();

        sb.AppendLine("## Key Metrics");
        sb.AppendLine();

        var indices = dataList.Where(d => d.DataType == DataType.Index).ToList();
        var commodities = dataList.Where(d => d.DataType == DataType.Commodity).Take(2).ToList();
        var currencies = dataList.Where(d => d.DataType == DataType.Currency).Take(2).ToList();

        sb.AppendLine("### Major Indices");
        sb.AppendLine();
        foreach (var index in indices.Take(4))
        {
            var changeIndicator = index.ChangePercent >= 0 ? "(+)" : "(-)";
            sb.AppendLine($"- **{index.Name}**: {index.ClosePrice:N2} {changeIndicator} {Math.Abs(index.ChangePercent ?? 0):F2}%");
        }
        sb.AppendLine();

        if (commodities.Any())
        {
            sb.AppendLine("### Commodities");
            sb.AppendLine();
            foreach (var commodity in commodities)
            {
                var changeIndicator = commodity.ChangePercent >= 0 ? "(+)" : "(-)";
                sb.AppendLine($"- **{commodity.Name}**: ${commodity.ClosePrice:N2} {changeIndicator} {Math.Abs(commodity.ChangePercent ?? 0):F2}%");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public string GenerateSectorPerformance(IEnumerable<MarketDataSnapshot> sectors)
    {
        var sb = new StringBuilder();
        var sectorList = sectors.OrderByDescending(s => s.ChangePercent).ToList();

        sb.AppendLine("## Sector Performance");
        sb.AppendLine();
        sb.AppendLine("| Sector | Close | Change % |");
        sb.AppendLine("|--------|------:|---------:|");

        foreach (var sector in sectorList)
        {
            var pctSign = sector.ChangePercent >= 0 ? "+" : "";
            sb.AppendLine($"| {sector.Name} | ${sector.ClosePrice:N2} | {pctSign}{sector.ChangePercent:F2}% |");
        }

        sb.AppendLine();

        // Top and bottom performers
        var topPerformer = sectorList.FirstOrDefault();
        var bottomPerformer = sectorList.LastOrDefault();

        if (topPerformer != null && bottomPerformer != null)
        {
            sb.AppendLine($"**Top Performer:** {topPerformer.Name} (+{topPerformer.ChangePercent:F2}%)");
            sb.AppendLine();
            sb.AppendLine($"**Bottom Performer:** {bottomPerformer.Name} ({bottomPerformer.ChangePercent:F2}%)");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public string GenerateCommoditiesSection(IEnumerable<MarketDataSnapshot> commodities)
    {
        var sb = new StringBuilder();
        var commodityList = commodities.ToList();

        sb.AppendLine("## Commodities");
        sb.AppendLine();
        sb.AppendLine("| Commodity | Price | Change | % Change |");
        sb.AppendLine("|-----------|------:|-------:|---------:|");

        foreach (var commodity in commodityList)
        {
            var changeSign = commodity.ChangeAmount >= 0 ? "+" : "";
            var pctSign = commodity.ChangePercent >= 0 ? "+" : "";
            sb.AppendLine($"| {commodity.Name} | ${commodity.ClosePrice:N2} | {changeSign}{commodity.ChangeAmount:N2} | {pctSign}{commodity.ChangePercent:F2}% |");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    public string GenerateCurrenciesSection(IEnumerable<MarketDataSnapshot> currencies)
    {
        var sb = new StringBuilder();
        var currencyList = currencies.ToList();

        sb.AppendLine("## Currencies");
        sb.AppendLine();
        sb.AppendLine("| Currency Pair | Rate | Change % |");
        sb.AppendLine("|---------------|-----:|---------:|");

        foreach (var currency in currencyList)
        {
            var pctSign = currency.ChangePercent >= 0 ? "+" : "";
            sb.AppendLine($"| {currency.Name} | {currency.ClosePrice:N4} | {pctSign}{currency.ChangePercent:F2}% |");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private string GenerateMarketOutlook(List<MarketDataSnapshot> data)
    {
        var indices = data.Where(d => d.DataType == DataType.Index).ToList();
        var avgChange = indices.Count > 0 ? indices.Average(i => i.ChangePercent ?? 0) : 0;

        var outlook = avgChange switch
        {
            > 1 => "Markets showed strong bullish momentum today with broad gains across major indices. Technical indicators suggest continued upward pressure in the near term.",
            > 0 => "Markets posted modest gains in today's session. The overall tone remains cautiously optimistic with investors watching for upcoming economic data.",
            > -1 => "Markets experienced slight weakness today with mixed trading. Traders remain focused on macroeconomic developments and corporate earnings.",
            _ => "Markets faced significant selling pressure today. Increased volatility may persist as investors reassess risk positions."
        };

        return outlook;
    }
}
