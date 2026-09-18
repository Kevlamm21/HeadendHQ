namespace HeadendHQ.Core.Shared;

public interface IReadModel
{
    Task<IReadOnlyList<TResult>> Search<TEntity, TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken)
        where TEntity : class;

    Task<int> Count<TEntity, TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken)
    where TEntity : class;
}
