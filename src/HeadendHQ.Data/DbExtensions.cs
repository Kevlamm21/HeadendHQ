using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using HeadendHQ.Data.Shared;
using HeadendHQ.DummyVideo;
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

    public static async Task InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await db.Database.MigrateAsync();

            if (!await db.Set<ScheduleScraperSettings>().AnyAsync())
                db.Add(new ScheduleScraperSettings());

            if (!await db.Set<DummyVideoSettings>().AnyAsync())
                db.Add(new DummyVideoSettings());

            if (!await db.Set<HdHomerunSettings>().AnyAsync())
                db.Add(new HdHomerunSettings());

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred initializing the database");
            throw;
        }
    }
}
