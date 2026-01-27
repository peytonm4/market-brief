using MarketBrief.Core.Enums;

namespace MarketBrief.Api.Models.Requests;

public record UpdateBriefRequest(
    string? Title = null,
    string? Summary = null,
    string? ContentMarkdown = null,
    BriefStatus? Status = null
);
