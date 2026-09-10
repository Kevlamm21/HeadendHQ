using HeadendHQ.AspNet;
using HeadendHQ.Data;
using HeadendHQ.FFmpeg;
using HeadendHQ.Hangfire;
using HeadendHQ.HdHomerun;
using HeadendHQ.Mediator;
using HeadendHQ.Nfo;
using HeadendHQ.SixLabors;
using HeadendHQ.VodLauncher;
using HeadendHQ.Web;
using HeadendHQ.Web.Infrastructure;
using HeadendHQ.WebScraping;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAspNet();
builder.ConfigureMediator(services => services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Transient;
}));

var dbPath = builder.Configuration["Database:Path"] ?? "/data/headendhq.db";

builder.ConfigureDatabase(dbPath);

builder.ConfigureHdHomerun();
builder.ConfigureNfo();
builder.ConfigureWebScraping();
builder.ConfigureVodLauncher();
builder.ConfigureFFmpeg();
builder.ConfigureSixLabors();
builder.ConfigureWeb();
builder.ConfigureHangfire();

var app = builder.Build();

var freshDatabase = await app.InitializeDatabase();

app.UseAspNet();
app.UseHangfireDashboard();
app.UseJobs(freshDatabase);
app.MapApi();

app.Run();
