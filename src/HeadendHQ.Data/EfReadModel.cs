using HeadendHQ.Core;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class EfReadModel(AppDbContext db) : IReadModel
{
    public async Task<IReadOnlyList<TResult>> Search<TEntity, TResult>(
        ISpecification<TEntity, TResult> spec, CancellationToken ct = default)
        where TEntity : class
    {
        return await spec.Apply(db.Set<TEntity>().AsNoTracking()).ToArrayAsync(ct);
    }
}
