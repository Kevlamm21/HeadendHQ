using HeadendHQ.AdbMapping;
using HeadendHQ.AdbMapping.Extractors;
using HeadendHQ.Core.HdHomerun;
using HeadendHQ.Core.SportingEvents;
using HeadendHQ.Data;
using HeadendHQ.DummyVideo;
using HeadendHQ.HdHomerun;
using HeadendHQ.Nba;
using HeadendHQ.Web.HdHomerun;
using HeadendHQ.Web.SportingEvents;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

// Database
var dbPath = builder.Configuration["Database:Path"] ?? "headendhq.db";
var dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir))
    Directory.CreateDirectory(dbDir);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// HDHomeRun
builder.Services.AddHttpClient<IHdHomerunService, HdHomerunService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; HeadendHQ/1.0)");
});
builder.Services.AddHostedService<HdHomerunXmltvJob>();

// Schedule scraper
builder.Services.Configure<ScheduleScraperOptions>(
    builder.Configuration.GetSection(ScheduleScraperOptions.SectionName));

builder.Services.AddScoped<NbaScheduleSource>();
builder.Services.AddTransient<IScheduleSource>(sp => sp.GetRequiredService<NbaScheduleSource>());

builder.Services.AddHostedService<ScheduleScraperJob>();

// Sporting event repository
builder.Services.AddScoped<ISportingEventRepository, SportingEventRepository>();

// ADB mapping
builder.Services.AddSingleton<IAdbExtractor, NbaExtractor>();
builder.Services.AddSingleton<IAdbExtractor, EspnExtractor>();
builder.Services.AddSingleton<IAdbExtractor, AmazonPrimeExtractor>();
builder.Services.AddSingleton<IAdbExtractor, PeacockExtractor>();
builder.Services.AddScoped<IAdbMappingService, AdbMappingService>();

// Dummy video
builder.Services.Configure<DummyVideoOptions>(
    builder.Configuration.GetSection(DummyVideoOptions.SectionName));
builder.Services.AddScoped<IVideoCreationService, VideoCreationService>();
builder.Services.AddScoped<IVideoCleanupService, VideoCleanupService>();
builder.Services.AddHostedService<DummyVideoJob>();

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

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
app.MapSportingEventEndpoints();
app.MapScheduleScraperEndpoints();
app.MapAdbMappingEndpoints();
app.MapDummyVideoEndpoints();

app.Run();
