using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new IptvGuideCacheConfiguration());
        modelBuilder.ApplyConfiguration(new IptvChannelConfiguration());
        modelBuilder.ApplyConfiguration(new IptvProgrammeConfiguration());
        modelBuilder.ApplyConfiguration(new TitleConfiguration());
        modelBuilder.ApplyConfiguration(new GlobalSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduleScrapingSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new VodLauncherSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new HdHomerunSettingsConfiguration());

        modelBuilder.ApplyConfiguration(new SourceSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new ImageConfiguration());
        modelBuilder.ApplyConfiguration(new SportConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueConfiguration());
        modelBuilder.ApplyConfiguration(new TeamConfiguration());
        modelBuilder.ApplyConfiguration(new BroadcasterConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogSyncStateConfiguration());
        modelBuilder.ApplyConfiguration(new SportingEventConfiguration());
    }
}
