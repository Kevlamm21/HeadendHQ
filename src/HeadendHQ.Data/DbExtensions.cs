
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using HeadendHQ.Data.Shared;
using HeadendHQ.VodLauncher;
using HeadendHQ.HdHomerun;
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

    /// <summary>
    /// Applies migrations and seeds the singleton rows. Returns <c>true</c> when this was a brand-new
    /// database (no migrations had been applied yet) — the caller uses that to decide whether to run
    /// the one-time catalog seed and broadcaster crawl.
    /// </summary>
    public static async Task<bool> InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // An empty history table (or a missing database file) means nothing has ever been
            // applied here — this is the first run against this volume.
            var freshDatabase = !(await db.Database.GetAppliedMigrationsAsync()).Any();

            await db.Database.MigrateAsync();

            if (!await db.Set<GlobalSettings>().AnyAsync())
                db.Add(new GlobalSettings());

            if (!await db.Set<ScheduleScrapingSettings>().AnyAsync())
                db.Add(new ScheduleScrapingSettings());

            if (!await db.Set<VodLauncherSettings>().AnyAsync())
                db.Add(new VodLauncherSettings());

            if (!await db.Set<HdHomerunSettings>().AnyAsync())
                db.Add(new HdHomerunSettings());

            await db.SaveChangesAsync();

            return freshDatabase;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred initializing the database");
            throw;
        }
    }
}
