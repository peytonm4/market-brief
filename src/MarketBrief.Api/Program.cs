using Hangfire;
using Hangfire.PostgreSql;
using MarketBrief.Api.Endpoints;
using MarketBrief.Api.Services;
using MarketBrief.Core.Enums;
using MarketBrief.Infrastructure.Data;
using MarketBrief.Infrastructure.External;
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

    if (app.Environment.IsDevelopment())
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

    // Set up recurring job for daily brief generation (6 AM Eastern, Mon-Fri)
    RecurringJob.AddOrUpdate<IBriefGenerationService>(
        "daily-market-brief",
        service => service.GenerateBriefAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TriggerType.Scheduled,
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
