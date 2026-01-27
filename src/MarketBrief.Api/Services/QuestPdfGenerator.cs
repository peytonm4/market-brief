using MarketBrief.Core.Entities;
using MarketBrief.Core.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MarketBrief.Api.Services;

public class QuestPdfGenerator : IPdfGenerator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuestPdfGenerator> _logger;

    public QuestPdfGenerator(IConfiguration configuration, ILogger<QuestPdfGenerator> logger)
    {
        _configuration = configuration;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData)
    {
        var dataList = marketData.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("MARKET BRIEF")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            col.Item().Text(brief.Title)
                                .FontSize(14)
                                .SemiBold();

                            col.Item().Text($"{brief.BriefDate:MMMM dd, yyyy}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(100).AlignRight().AlignMiddle().Text("DAILY REPORT")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Darken3);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(15);

                    // Executive Summary
                    if (!string.IsNullOrEmpty(brief.Summary))
                    {
                        column.Item().Column(section =>
                        {
                            section.Item().Text("Executive Summary")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            section.Item().PaddingTop(5).Text(brief.Summary)
                                .FontSize(10)
                                .LineHeight(1.4f);
                        });
                    }

                    // Market Indices
                    var indices = dataList.Where(d => d.DataType == DataType.Index).ToList();
                    if (indices.Any())
                    {
                        column.Item().Column(section =>
                        {
                            section.Item().Text("Major Indices")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            section.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Index").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Close").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Change").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("% Change").Bold();
                                });

                                foreach (var index in indices)
                                {
                                    var changeColor = index.ChangePercent >= 0 ? Colors.Green.Darken1 : Colors.Red.Darken1;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(index.Name);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{index.ClosePrice:N2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                        .Text($"{(index.ChangeAmount >= 0 ? "+" : "")}{index.ChangeAmount:N2}").FontColor(changeColor);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                        .Text($"{(index.ChangePercent >= 0 ? "+" : "")}{index.ChangePercent:F2}%").FontColor(changeColor);
                                }
                            });
                        });
                    }

                    // Sector Performance
                    var sectors = dataList.Where(d => d.DataType == DataType.Sector).OrderByDescending(s => s.ChangePercent).ToList();
                    if (sectors.Any())
                    {
                        column.Item().Column(section =>
                        {
                            section.Item().Text("Sector Performance")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            section.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Sector").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Close").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("% Change").Bold();
                                });

                                foreach (var sector in sectors)
                                {
                                    var changeColor = sector.ChangePercent >= 0 ? Colors.Green.Darken1 : Colors.Red.Darken1;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(sector.Name);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${sector.ClosePrice:N2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                        .Text($"{(sector.ChangePercent >= 0 ? "+" : "")}{sector.ChangePercent:F2}%").FontColor(changeColor);
                                }
                            });
                        });
                    }

                    // Commodities
                    var commodities = dataList.Where(d => d.DataType == DataType.Commodity).ToList();
                    if (commodities.Any())
                    {
                        column.Item().Column(section =>
                        {
                            section.Item().Text("Commodities")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            section.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Commodity").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Price").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("% Change").Bold();
                                });

                                foreach (var commodity in commodities)
                                {
                                    var changeColor = commodity.ChangePercent >= 0 ? Colors.Green.Darken1 : Colors.Red.Darken1;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(commodity.Name);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${commodity.ClosePrice:N2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                        .Text($"{(commodity.ChangePercent >= 0 ? "+" : "")}{commodity.ChangePercent:F2}%").FontColor(changeColor);
                                }
                            });
                        });
                    }

                    // Currencies
                    var currencies = dataList.Where(d => d.DataType == DataType.Currency).ToList();
                    if (currencies.Any())
                    {
                        column.Item().Column(section =>
                        {
                            section.Item().Text("Currencies")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            section.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Pair").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Rate").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("% Change").Bold();
                                });

                                foreach (var currency in currencies)
                                {
                                    var changeColor = currency.ChangePercent >= 0 ? Colors.Green.Darken1 : Colors.Red.Darken1;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(currency.Name);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{currency.ClosePrice:N4}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                        .Text($"{(currency.ChangePercent >= 0 ? "+" : "")}{currency.ChangePercent:F2}%").FontColor(changeColor);
                                }
                            });
                        });
                    }
                });

                page.Footer().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().AlignLeft().Text(text =>
                    {
                        text.Span("Generated by Market Brief System").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" of ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<string> GenerateAndSavePdfAsync(MarketBriefEntity brief, IEnumerable<MarketDataSnapshot> marketData, CancellationToken cancellationToken = default)
    {
        var pdfBytes = GeneratePdf(brief, marketData);

        var storagePath = _configuration["Storage:PdfPath"] ?? "pdfs";
        Directory.CreateDirectory(storagePath);

        var fileName = $"market-brief-{brief.BriefDate:yyyy-MM-dd}.pdf";
        var filePath = Path.Combine(storagePath, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);

        _logger.LogInformation("PDF saved to {FilePath}", filePath);
        return filePath;
    }
}
