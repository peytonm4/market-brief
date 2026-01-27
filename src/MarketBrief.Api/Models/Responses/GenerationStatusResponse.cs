namespace MarketBrief.Api.Models.Responses;

public record GenerationStatusResponse(
    bool IsRunning,
    string? CurrentJobId,
    string? Status,
    DateTime? StartedAt,
    Guid? BriefId,
    string? Message
);

public record GenerationHistoryResponse(
    Guid Id,
    Guid? BriefId,
    string? JobId,
    string TriggerType,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage
);

public record TriggerGenerationResponse(
    string JobId,
    string Message,
    DateOnly TargetDate
);
