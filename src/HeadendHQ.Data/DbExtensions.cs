using HeadendHQ.Core.Assets;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Data.Shared;
using HeadendHQ.VodLauncher;
using HeadendHQ.HdHomerun;
using HeadendHQ.ScheduleScraping;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Data;

public static class DbExtensions
{
    public static void ConfigureDatabase(this WebApplicationBuilder builder, string dbPath)
    {
        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir))
            Directory.CreateDirectory(dbDir);

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddScoped<IReadModel, EfReadModel<AppDbContext>>();
        builder.Services.AddScoped<IWorkspace, EfWorkspace<AppDbContext>>();
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<AppDbContext>>();
    }

    public static async Task InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await db.Database.MigrateAsync();

            if (!await db.Set<GlobalSettings>().AnyAsync())
                db.Add(new GlobalSettings());

            if (!await db.Set<ScheduleScrapingSettings>().AnyAsync())
                db.Add(new ScheduleScrapingSettings());

            if (!await db.Set<VodLauncherSettings>().AnyAsync())
                db.Add(new VodLauncherSettings());

            if (!await db.Set<HdHomerunSettings>().AnyAsync())
                db.Add(new HdHomerunSettings());

            await SeedAssets(db);

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred initializing the database");
            throw;
        }
    }

    private static async Task SeedAssets(AppDbContext db)
    {
        foreach (League league in Enum.GetValues<League>())
        {
            if (!await db.Set<LeagueAsset>().AnyAsync(a => a.League == league && a.Variant == "Default"))
                db.Add(new LeagueAsset(league, "Default"));

            if (!await db.Set<WordMark>().AnyAsync(w => w.League == league && w.Variant == "Original"))
                db.Add(new WordMark(league, "Original"));
        }

        foreach (StreamingService service in Enum.GetValues<StreamingService>())
        {
            if (!await db.Set<StreamingServiceAsset>().AnyAsync(a => a.Service == service))
                db.Add(new StreamingServiceAsset(service));
        }

        foreach (var (name, league, primary, secondary) in Teams)
        {
            if (!await db.Set<TeamAsset>().AnyAsync(t => t.TeamName == name && t.League == league))
            {
                var asset = new TeamAsset(name, league);
                asset.Update(name, league, primary, secondary, null);
                db.Add(asset);
            }
        }
    }

    private static readonly (string Name, League League, string? Primary, string? Secondary)[] Teams =
    [
        // NBA
        ("Atlanta Hawks",           League.Nba, "#e03a3e", "#26282a"),
        ("Boston Celtics",          League.Nba, "#007a33", "#ba9653"),
        ("Brooklyn Nets",           League.Nba, "#000000", "#ffffff"),
        ("Charlotte Hornets",       League.Nba, "#1d1160", "#00788c"),
        ("Chicago Bulls",           League.Nba, "#ce1141", "#000000"),
        ("Cleveland Cavaliers",     League.Nba, "#860038", "#041e42"),
        ("Dallas Mavericks",        League.Nba, "#002b5e", "#b8c4ca"),
        ("Denver Nuggets",          League.Nba, "#0e2240", "#8b2131"),
        ("Detroit Pistons",         League.Nba, "#c8102e", "#1d42ba"),
        ("Golden State Warriors",   League.Nba, "#ffc72c", "#1d428a"),
        ("Houston Rockets",         League.Nba, "#000000", "#ce1141"),
        ("Indiana Pacers",          League.Nba, "#fdbb30", "#002d62"),
        ("Los Angeles Clippers",    League.Nba, "#1d428a", "#c8102e"),
        ("Los Angeles Lakers",      League.Nba, "#552583", "#f9a01b"),
        ("Memphis Grizzlies",       League.Nba, "#5d76a9", "#f5b112"),
        ("Miami Heat",              League.Nba, "#98002e", "#000000"),
        ("Milwaukee Bucks",         League.Nba, "#00471b", "#eee1c6"),
        ("Minnesota Timberwolves",  League.Nba, "#0c2340", "#78be20"),
        ("New Orleans Pelicans",    League.Nba, "#0c2340", "#c8102e"),
        ("New York Knicks",         League.Nba, "#006bb6", "#f58426"),
        ("Oklahoma City Thunder",   League.Nba, "#002d62", "#ef3b24"),
        ("Orlando Magic",           League.Nba, "#0077c0", "#c4ced4"),
        ("Philadelphia 76ers",      League.Nba, "#006bb6", "#ed174c"),
        ("Phoenix Suns",            League.Nba, "#1d1160", "#e56020"),
        ("Portland Trail Blazers",  League.Nba, "#e03a3e", "#000000"),
        ("Sacramento Kings",        League.Nba, "#5a2d81", "#63727a"),
        ("San Antonio Spurs",       League.Nba, "#c4ced4", "#000000"),
        ("Toronto Raptors",         League.Nba, "#000000", "#ce1141"),
        ("Utah Jazz",               League.Nba, "#7DAEE3", "#000000"),
        ("Washington Wizards",      League.Nba, "#e31837", "#002b5c"),
    ];
}
