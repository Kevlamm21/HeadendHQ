using HeadendHQ.Core.Shared;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data.Shared;

public class EfWorkspace<TContext>(TContext context) : IWorkspace
    where TContext : DbContext
{
    public async Task<IReadOnlyList<T>> Load<T>(ISpecification<T> spec, CancellationToken ct = default)
        where T : class
    {
        return await spec.Apply(context.Set<T>().AsTracking().AsSplitQuery()).ToListAsync(ct);
    }

    public void Add<T>(T entity) where T : class => context.Add(entity);
    public void Remove<T>(T entity) where T : class => context.Remove(entity);
}
