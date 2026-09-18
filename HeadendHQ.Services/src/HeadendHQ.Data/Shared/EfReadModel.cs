using HeadendHQ.Core.Shared;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data.Shared;

public class EfReadModel<TContext>(TContext context) : IReadModel
    where TContext : DbContext
{
    public async Task<IReadOnlyList<TResult>> Search<TEntity, TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken)
        where TEntity : class
    {
        var queryable = context.Set<TEntity>()
            .AsNoTracking()
            .AsSplitQuery();

        return await specification.Apply(queryable).ToArrayAsync(cancellationToken);
    }

    public async Task<int> Count<TEntity, TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken)
        where TEntity : class
    {
        var queryable = context.Set<TEntity>()
            .AsNoTracking()
            .AsSplitQuery();

        return await specification.Apply(queryable).CountAsync(cancellationToken);
    }
}
