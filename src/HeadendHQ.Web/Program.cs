using HeadendHQ.AmazonPrime;
using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Data;
using HeadendHQ.DummyVideo;
using HeadendHQ.Espn;
using HeadendHQ.HdHomerun;
using HeadendHQ.Mediator;
using HeadendHQ.Nba;
using HeadendHQ.Peacock;
using HeadendHQ.Web.CronJobs;
using HeadendHQ.Web.HdHomerun;
using HeadendHQ.Web.Settings;
using HeadendHQ.Web.Titles;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureMediator(services => services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Transient;
}));

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Database
var dbPath = builder.Configuration["Database:Path"];
builder.ConfigureDatabase(dbPath);

// HDHomeRun
builder.Services.AddHttpClient<IHdHomerunService, HdHomerunService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; HeadendHQ/1.0)");
});
builder.Services.AddHostedService<HdHomerunXmltvJob>();

// Schedule scraper
builder.Services.AddScoped<NbaScheduleScraper>();
builder.Services.AddTransient<IScheduleScraper>(sp => sp.GetRequiredService<NbaScheduleScraper>());
builder.Services.AddHostedService<ScheduleScraperJob>();

// ADB mapping
builder.Services.AddSingleton<NbaLinkResolver>();
builder.Services.AddSingleton<IAdbExtractor, NbaExtractor>();
builder.Services.AddSingleton<EspnLinkResolver>();
builder.Services.AddSingleton<IAdbExtractor, EspnExtractor>();
builder.Services.AddSingleton<PeacockLinkResolver>();
builder.Services.AddSingleton<IAdbExtractor, PeacockExtractor>();
builder.Services.AddSingleton<AmazonPrimeLinkResolver>();
builder.Services.AddSingleton<IAdbExtractor, AmazonPrimeExtractor>();
builder.Services.AddScoped<AdbMappingService>();

// Dummy video
builder.Services.AddScoped<ICreationService, VideoCreationService>();
builder.Services.AddScoped<ICleanupService, VideoCleanupService>();
builder.Services.AddHostedService<DummyVideoJob>();

var app = builder.Build();

await app.InitializeDatabase();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
    .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithTags("Health")
    .WithName("GetHealth")
    .WithSummary("Health check")
    .WithDescription("Returns the current health status of the API.");

app.MapHdHomerunEndpoints();
app.MapTitleEndpoints();
app.MapScheduleScraperEndpoints();
app.MapAdbMappingEndpoints();
app.MapDummyVideoEndpoints();
app.MapSettingsEndpoints();

app.Run();
