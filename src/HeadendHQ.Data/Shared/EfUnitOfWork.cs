using HeadendHQ.Core.Shared;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data.Shared;

public class EfUnitOfWork<TContext>(TContext context) : IUnitOfWork
    where TContext : DbContext
{
    public Task SaveChanges(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
