using System.Text.Json;
using MarketBrief.Api.Models.Requests;
using MarketBrief.Api.Models.Responses;
using MarketBrief.Api.Services;
using MarketBrief.Core.Entities;
using MarketBrief.Core.Enums;
using MarketBrief.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketBrief.Api.Endpoints;

public static class BriefsEndpoints
{
    public static IEndpointRouteBuilder MapBriefsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/briefs")
            .WithTags("Briefs");

        group.MapGet("/", GetBriefs)
            .WithName("GetBriefs")
            .WithDescription("Get paginated list of briefs");

        group.MapGet("/latest", GetLatestBrief)
            .WithName("GetLatestBrief")
            .WithDescription("Get the most recent brief");

        group.MapGet("/{id:guid}", GetBriefById)
            .WithName("GetBriefById")
            .WithDescription("Get a brief by ID");

        group.MapGet("/date/{date}", GetBriefByDate)
            .WithName("GetBriefByDate")
            .WithDescription("Get a brief by date");

        group.MapPost("/", CreateBrief)
            .WithName("CreateBrief")
            .WithDescription("Create a new brief");

        group.MapPut("/{id:guid}", UpdateBrief)
            .WithName("UpdateBrief")
            .WithDescription("Update an existing brief");

        group.MapDelete("/{id:guid}", DeleteBrief)
            .WithName("DeleteBrief")
            .WithDescription("Delete a brief");

        // Format endpoints
        group.MapGet("/{id:guid}/json", GetBriefAsJson)
            .WithName("GetBriefAsJson")
            .WithDescription("Get brief content as structured JSON");

        group.MapGet("/{id:guid}/markdown", GetBriefAsMarkdown)
            .WithName("GetBriefAsMarkdown")
            .WithDescription("Get brief content as Markdown");

        group.MapGet("/{id:guid}/pdf", GetBriefAsPdf)
            .WithName("GetBriefAsPdf")
            .WithDescription("Download brief as PDF");

        group.MapPost("/{id:guid}/pdf/regenerate", RegeneratePdf)
            .WithName("RegeneratePdf")
            .WithDescription("Regenerate the PDF for a brief");

        return app;
    }

    private static async Task<IResult> GetBriefs(
        MarketBriefDbContext dbContext,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.MarketBriefs.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var briefs = await query
            .OrderByDescending(b => b.BriefDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BriefResponse(
                b.Id,
                b.BriefDate,
                b.Title,
                b.Summary,
                b.Status.ToString(),
                b.PdfStoragePath != null,
                b.CreatedAt,
                b.UpdatedAt,
                b.PublishedAt
            ))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PaginatedResponse<BriefResponse>(
            briefs,
            page,
            pageSize,
            totalCount,
            totalPages
        ));
    }

    private static async Task<IResult> GetLatestBrief(
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .AsNoTracking()
            .Include(b => b.Sections.OrderBy(s => s.DisplayOrder))
            .OrderByDescending(b => b.BriefDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = "No briefs found" });
        }

        return Results.Ok(MapToDetailResponse(brief));
    }

    private static async Task<IResult> GetBriefById(
        Guid id,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .AsNoTracking()
            .Include(b => b.Sections.OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = $"Brief {id} not found" });
        }

        return Results.Ok(MapToDetailResponse(brief));
    }

    private static async Task<IResult> GetBriefByDate(
        DateOnly date,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .AsNoTracking()
            .Include(b => b.Sections.OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(b => b.BriefDate == date, cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = $"Brief for {date} not found" });
        }

        return Results.Ok(MapToDetailResponse(brief));
    }

    private static async Task<IResult> CreateBrief(
        CreateBriefRequest request,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.MarketBriefs
            .AnyAsync(b => b.BriefDate == request.BriefDate, cancellationToken);

        if (existing)
        {
            return Results.Conflict(new { message = $"Brief for {request.BriefDate} already exists" });
        }

        var brief = new MarketBriefEntity
        {
            Id = Guid.NewGuid(),
            BriefDate = request.BriefDate,
            Title = request.Title,
            Summary = request.Summary,
            Status = BriefStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.MarketBriefs.Add(brief);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/briefs/{brief.Id}", new BriefResponse(
            brief.Id,
            brief.BriefDate,
            brief.Title,
            brief.Summary,
            brief.Status.ToString(),
            false,
            brief.CreatedAt,
            brief.UpdatedAt,
            brief.PublishedAt
        ));
    }

    private static async Task<IResult> UpdateBrief(
        Guid id,
        UpdateBriefRequest request,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = $"Brief {id} not found" });
        }

        if (request.Title != null)
        {
            brief.Title = request.Title;
        }

        if (request.Summary != null)
        {
            brief.Summary = request.Summary;
        }

        if (request.ContentMarkdown != null)
        {
            brief.ContentMarkdown = request.ContentMarkdown;
        }

        if (request.Status.HasValue)
        {
            brief.Status = request.Status.Value;
            if (request.Status.Value == BriefStatus.Published)
            {
                brief.PublishedAt = DateTime.UtcNow;
            }
        }

        brief.UpdatedAt = DateTime.UtcNow;
        brief.Version++;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new BriefResponse(
            brief.Id,
            brief.BriefDate,
            brief.Title,
            brief.Summary,
            brief.Status.ToString(),
            brief.PdfStoragePath != null,
            brief.CreatedAt,
            brief.UpdatedAt,
            brief.PublishedAt
        ));
    }

    private static async Task<IResult> DeleteBrief(
        Guid id,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = $"Brief {id} not found" });
        }

        dbContext.MarketBriefs.Remove(brief);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetBriefAsJson(
        Guid id,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = $"Brief {id} not found" });
        }

        if (string.IsNullOrEmpty(brief.ContentJson))
        {
            return Results.NotFound(new { message = "No JSON content available" });
        }

        var content = JsonSerializer.Deserialize<object>(brief.ContentJson);
        return Results.Ok(content);
    }

    private static async Task<IResult> GetBriefAsMarkdown(
        Guid id,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = $"Brief {id} not found" });
        }

        if (string.IsNullOrEmpty(brief.ContentMarkdown))
        {
            return Results.NotFound(new { message = "No markdown content available" });
        }

        return Results.Text(brief.ContentMarkdown, "text/markdown");
    }

    private static async Task<IResult> GetBriefAsPdf(
        Guid id,
        MarketBriefDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var brief = await dbContext.MarketBriefs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
        {
            return Results.NotFound(new { message = $"Brief {id} not found" });
        }

        if (string.IsNullOrEmpty(brief.PdfStoragePath) || !File.Exists(brief.PdfStoragePath))
        {
            return Results.NotFound(new { message = "PDF not available" });
        }

        var fileBytes = await File.ReadAllBytesAsync(brief.PdfStoragePath, cancellationToken);
        var fileName = $"market-brief-{brief.BriefDate:yyyy-MM-dd}.pdf";

        return Results.File(fileBytes, "application/pdf", fileName);
    }

    private static async Task<IResult> RegeneratePdf(
        Guid id,
        IBriefGenerationService generationService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pdfPath = await generationService.RegeneratePdfAsync(id, cancellationToken);
            return Results.Ok(new { message = "PDF regenerated successfully", path = pdfPath });
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
    }

    private static BriefDetailResponse MapToDetailResponse(MarketBriefEntity brief)
    {
        object? contentJson = null;
        if (!string.IsNullOrEmpty(brief.ContentJson))
        {
            try
            {
                contentJson = JsonSerializer.Deserialize<object>(brief.ContentJson);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new BriefDetailResponse(
            brief.Id,
            brief.BriefDate,
            brief.Title,
            brief.Summary,
            brief.ContentMarkdown,
            contentJson,
            brief.Status.ToString(),
            brief.PdfStoragePath,
            brief.PdfGeneratedAt,
            brief.GenerationStartedAt,
            brief.GenerationCompletedAt,
            brief.GenerationDurationMs,
            brief.CreatedAt,
            brief.UpdatedAt,
            brief.PublishedAt,
            brief.Version,
            brief.Sections.Select(s => new BriefSectionResponse(
                s.Id,
                s.SectionType.ToString(),
                s.Title,
                s.ContentMarkdown,
                string.IsNullOrEmpty(s.ContentJson) ? null : JsonSerializer.Deserialize<object>(s.ContentJson),
                s.DisplayOrder
            ))
        );
    }
}
