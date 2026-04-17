using HeadendHQ.Core.HdHomerun;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<HdHomerunDevice> HdHomerunDevices => Set<HdHomerunDevice>();
}
