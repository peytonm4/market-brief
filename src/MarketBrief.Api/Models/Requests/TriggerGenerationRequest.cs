namespace MarketBrief.Api.Models.Requests;

public record TriggerGenerationRequest(DateOnly? Date = null, bool Force = false);
