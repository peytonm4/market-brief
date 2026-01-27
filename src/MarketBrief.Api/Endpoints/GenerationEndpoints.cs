using Hangfire;
using MarketBrief.Api.Models.Requests;
using MarketBrief.Api.Models.Responses;
using MarketBrief.Api.Services;
using MarketBrief.Core.Enums;
using MarketBrief.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketBrief.Api.Endpoints;

public static class GenerationEndpoints
{
    public static IEndpointRouteBuilder MapGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/generation")
            .WithTags("Generation");

        group.MapPost("/trigger", TriggerGeneration)
            .WithName("TriggerGeneration")
            .WithDescription("Trigger manual brief generation");

        group.MapGet("/status", GetGenerationStatus)
            .WithName("GetGenerationStatus")
            .WithDescription("Get current generation status");

        group.MapGet("/history", GetGenerationHistory)
            .WithName("GetGenerationHistory")
            .WithDescription("Get generation history");

        return app;
    }

    private static IResult TriggerGeneration(
        TriggerGenerationRequest request,
        IBackgroundJobClient backgroundJobClient,
        IBriefGenerationService generationService)
    {
        if (generationService.IsGenerationRunning())
        {
            return Results.Conflict(new { message = "Generation is already in progress" });
        }

        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var jobId = backgroundJobClient.Enqueue<IBriefGenerationService>(
            service => service.GenerateBriefAsync(targetDate, TriggerType.Manual, CancellationToken.None));

        return Results.Accepted(
            $"/api/v1/generation/status",
            new TriggerGenerationResponse(jobId, "Generation started", targetDate));
    }

    private static IResult GetGenerationStatus(IBriefGenerationService generationService)
    {
        var isRunning = generationService.IsGenerationRunning();
        var currentStatus = generationService.GetCurrentGenerationStatus();

        return Results.Ok(new GenerationStatusResponse(
            isRunning,
            currentStatus?.JobId,
            currentStatus?.Status.ToString(),
            currentStatus?.StartedAt,
            currentStatus?.BriefId,
            isRunning ? "Generation in progress" : "No generation running"
        ));
    }

    private static async Task<IResult> GetGenerationHistory(
        MarketBriefDbContext dbContext,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.GenerationLogs.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await query
            .OrderByDescending(l => l.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new GenerationHistoryResponse(
                l.Id,
                l.BriefId,
                l.JobId,
                l.TriggerType.ToString(),
                l.Status.ToString(),
                l.StartedAt,
                l.CompletedAt,
                l.ErrorMessage
            ))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PaginatedResponse<GenerationHistoryResponse>(
            logs,
            page,
            pageSize,
            totalCount,
            totalPages
        ));
    }
}
