using HeadendHQ.Core.HdHomerun;
using HeadendHQ.Data;
using HeadendHQ.HdHomerun;
using HeadendHQ.Web.HdHomerun;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Database
var dbPath = builder.Configuration["Database:Path"] ?? "headendhq.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// HDHomeRun
builder.Services.AddHttpClient<IHdHomerunService, HdHomerunService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; HeadendHQ/1.0)");
});
builder.Services.AddHostedService<HdHomerunXmltvJob>();

var app = builder.Build();

// Ensure database is created on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
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

app.Run();
