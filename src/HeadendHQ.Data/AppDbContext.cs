using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new XmltvCacheConfiguration());
        modelBuilder.ApplyConfiguration(new TitleConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduleScraperSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new DummyVideoSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new HdHomerunSettingsConfiguration());
    }
}
