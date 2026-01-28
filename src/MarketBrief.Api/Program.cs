using Hangfire;
using Hangfire.PostgreSql;
using MarketBrief.Api.Configuration;
using MarketBrief.Api.Endpoints;
using MarketBrief.Api.Services;
using MarketBrief.Api.Services.News;
using MarketBrief.Core.Enums;
using MarketBrief.Infrastructure.Data;
using MarketBrief.Infrastructure.External;
using MarketBrief.Infrastructure.External.Gdelt;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Database
builder.Services.AddDbContext<MarketBriefDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
    options.Queues = new[] { "default", "generation" };
});

// HTTP Client for market data
builder.Services.AddHttpClient<IMarketDataApiClient, MarketDataApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MarketDataApi:BaseUrl"] ?? "https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// GDELT configuration and HTTP Client
builder.Services.Configure<GdeltOptions>(builder.Configuration.GetSection(GdeltOptions.SectionName));
var gdeltTimeout = builder.Configuration.GetValue<int>("Gdelt:RequestTimeoutSeconds", 30);
builder.Services.AddHttpClient<IGdeltNewsClient, GdeltNewsClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(gdeltTimeout);
});

// News processing services
builder.Services.AddScoped<INewsImpactCalculator, NewsImpactCalculator>();
builder.Services.AddScoped<INewsDeduplicationService, NewsDeduplicationService>();
builder.Services.AddScoped<INewsRankingService, NewsRankingService>();

// Services
builder.Services.AddScoped<IBriefGenerationService, BriefGenerationService>();
builder.Services.AddScoped<IMarkdownGenerator, MarkdownGenerator>();
builder.Services.AddScoped<IPdfGenerator, QuestPdfGenerator>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5173", "http://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Market Brief API",
        Version = "v1",
        Description = "API for generating and managing daily market briefs"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Market Brief API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors();

// Map endpoints
app.MapApiEndpoints();

// Hangfire Dashboard
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

// Apply migrations and set up recurring jobs
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MarketBriefDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (app.Environment.IsDevelopment())
    {
        await dbContext.Database.EnsureCreatedAsync();

        // Apply news entities migration if tables don't exist
        // Use try-catch since SqlQueryRaw<T> is EF Core 8+ only
        var tablesExist = false;
        try
        {
            // Check if news_story_clusters table exists by querying it
            await dbContext.NewsStoryClusters.AnyAsync();
            tablesExist = true;
        }
        catch (Exception)
        {
            tablesExist = false;
        }

        if (!tablesExist)
        {
            logger.LogInformation("Creating news entities tables...");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS news_story_clusters (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    brief_id uuid NOT NULL,
                    primary_headline character varying(1000) NOT NULL,
                    why_it_matters character varying(2000) NULL,
                    query_bucket_name character varying(100) NOT NULL,
                    impact_score numeric(10,4) NOT NULL,
                    pickup_score numeric(10,4) NOT NULL,
                    recency_score numeric(10,4) NOT NULL,
                    relevance_score numeric(10,4) NOT NULL,
                    final_score numeric(10,4) NOT NULL,
                    display_order integer NOT NULL DEFAULT 0,
                    article_count integer NOT NULL DEFAULT 0,
                    representative_sources_json jsonb NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT ""PK_news_story_clusters"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_news_story_clusters_market_briefs_brief_id"" FOREIGN KEY (brief_id) REFERENCES market_briefs(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS news_articles (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    gdelt_url character varying(2048) NOT NULL,
                    title character varying(1000) NOT NULL,
                    snippet character varying(2000) NULL,
                    source_domain character varying(500) NOT NULL,
                    published_at timestamp with time zone NOT NULL,
                    query_bucket_name character varying(100) NOT NULL,
                    tone numeric(10,4) NULL,
                    cluster_id uuid NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT ""PK_news_articles"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_news_articles_news_story_clusters_cluster_id"" FOREIGN KEY (cluster_id) REFERENCES news_story_clusters(id) ON DELETE SET NULL
                );

                CREATE INDEX IF NOT EXISTS ""IX_news_story_clusters_brief_id"" ON news_story_clusters (brief_id);
                CREATE INDEX IF NOT EXISTS ""IX_news_story_clusters_brief_id_display_order"" ON news_story_clusters (brief_id, display_order);
                CREATE INDEX IF NOT EXISTS ""IX_news_articles_cluster_id"" ON news_articles (cluster_id);
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_news_articles_gdelt_url"" ON news_articles (gdelt_url);
                CREATE INDEX IF NOT EXISTS ""IX_news_articles_published_at"" ON news_articles (published_at);
                CREATE INDEX IF NOT EXISTS ""IX_news_articles_query_bucket_name"" ON news_articles (query_bucket_name);
            ");
            logger.LogInformation("News entities tables created successfully");
        }
        else
        {
            logger.LogInformation("News entities tables already exist");
        }
    }

    // Set up recurring job for daily brief generation (6 AM Eastern, Mon-Fri)
    RecurringJob.AddOrUpdate<IBriefGenerationService>(
        "daily-market-brief",
        service => service.GenerateBriefAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TriggerType.Scheduled,
            false,
            CancellationToken.None),
        "0 6 * * 1-5",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
        });
}

app.Run();

// Hangfire authorization filter (allow all in development, restrict in production)
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In production, implement proper authorization
        return true;
    }
}
