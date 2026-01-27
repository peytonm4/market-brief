namespace MarketBrief.Api.Models.Requests;

public record CreateBriefRequest(
    DateOnly BriefDate,
    string Title,
    string? Summary = null
);
