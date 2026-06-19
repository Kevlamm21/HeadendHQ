using HeadendHQ.AmazonPrime;
using HeadendHQ.AspNet;
using HeadendHQ.Data;
using HeadendHQ.Espn;
using HeadendHQ.FFmpeg;
using HeadendHQ.Hangfire;
using HeadendHQ.HdHomerun;
using HeadendHQ.Mediator;
using HeadendHQ.Nba;
using HeadendHQ.Nfo;
using HeadendHQ.Peacock;
using HeadendHQ.ScheduleScraping;
using HeadendHQ.SixLabors;
using HeadendHQ.VodLauncher;
using HeadendHQ.Web;
using HeadendHQ.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAspNet();
builder.ConfigureMediator(services => services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Transient;
}));

var dbPath = builder.Configuration["Database:Path"] ?? "/data/headendhq.db";

builder.ConfigureDatabase(dbPath);

builder.ConfigureHdHomerun();
builder.ConfigureNba();
builder.ConfigureNfo();
builder.ConfigureEspn();
builder.ConfigurePeacock();
builder.ConfigureAmazonPrime();
builder.ConfigureScheduleScraping();
builder.ConfigureVodLauncher();
builder.ConfigureFFmpeg();
builder.ConfigureSixLabors();
builder.ConfigureWeb();
builder.ConfigureHangfire();

var app = builder.Build();

await app.InitializeDatabase();

app.UseAspNet();
app.UseHangfireDashboard();
app.MapApi();

app.Run();
