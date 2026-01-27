using MarketBrief.Core.Enums;

namespace MarketBrief.Api.Models.Responses;

public record BriefResponse(
    Guid Id,
    DateOnly BriefDate,
    string Title,
    string? Summary,
    string Status,
    bool HasPdf,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt
);

public record BriefDetailResponse(
    Guid Id,
    DateOnly BriefDate,
    string Title,
    string? Summary,
    string? ContentMarkdown,
    object? ContentJson,
    string Status,
    string? PdfStoragePath,
    DateTime? PdfGeneratedAt,
    DateTime? GenerationStartedAt,
    DateTime? GenerationCompletedAt,
    int? GenerationDurationMs,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt,
    int Version,
    IEnumerable<BriefSectionResponse> Sections
);

public record BriefSectionResponse(
    Guid Id,
    string SectionType,
    string Title,
    string? ContentMarkdown,
    object? ContentJson,
    int DisplayOrder
);

public record PaginatedResponse<T>(
    IEnumerable<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
