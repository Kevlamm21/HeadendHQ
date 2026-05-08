using HeadendHQ.Core.HdHomerun;
using HeadendHQ.Core.SportingEvents;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<HdHomerunDevice> HdHomerunDevices => Set<HdHomerunDevice>();
    public DbSet<SportingEvent> SportingEvents => Set<SportingEvent>();
}
