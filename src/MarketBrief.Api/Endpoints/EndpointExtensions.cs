namespace MarketBrief.Api.Endpoints;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapBriefsEndpoints();
        app.MapGenerationEndpoints();
        app.MapMarketDataEndpoints();

        return app;
    }
}
