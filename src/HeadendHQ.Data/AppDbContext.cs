using HeadendHQ.Core.Options;
using HeadendHQ.Core.Titles;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<HdHomerunDeviceOptions> HdHomerunDevices => Set<HdHomerunDeviceOptions>();
    public DbSet<Title> Titles => Set<Title>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Title>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsOne(e => e.Metadata);
        });
    }
}
