using HeadendHQ.Core;

namespace HeadendHQ.Data;

public class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChanges(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
