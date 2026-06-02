using HeadendHQ.Core;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class EfWorkspace(AppDbContext db) : IWorkspace
{
    public async Task<IReadOnlyList<T>> Load<T>(ISpecification<T> spec, CancellationToken ct = default)
        where T : class
    {
        return await spec.Apply(db.Set<T>().AsTracking()).ToListAsync(ct);
    }

    public void Add<T>(T entity) where T : class => db.Add(entity);
    public void Remove<T>(T entity) where T : class => db.Remove(entity);
}
