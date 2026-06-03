namespace HeadendHQ.Core.Shared;

public interface IWorkspace
{
    Task<IReadOnlyList<T>> Load<T>(ISpecification<T> spec, CancellationToken ct = default)
        where T : class;

    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
}
