namespace HeadendHQ.Core;

public interface IReadModel
{
    Task<IReadOnlyList<TResult>> Search<TEntity, TResult>(
        ISpecification<TEntity, TResult> spec, CancellationToken ct = default)
        where TEntity : class;
}
