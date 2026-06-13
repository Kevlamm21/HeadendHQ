using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new XmltvCacheConfiguration());
        modelBuilder.ApplyConfiguration(new TitleConfiguration());
        modelBuilder.ApplyConfiguration(new GlobalSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduleScrapingSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new VodLauncherSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new HdHomerunSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueAssetConfiguration());
        modelBuilder.ApplyConfiguration(new TeamAssetConfiguration());
        modelBuilder.ApplyConfiguration(new StreamingServiceAssetConfiguration());
        modelBuilder.ApplyConfiguration(new WordMarkConfiguration());
    }
}
