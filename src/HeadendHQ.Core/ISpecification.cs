namespace HeadendHQ.Core;

public interface ISpecification<TSource, TResult>
{
    IQueryable<TResult> Apply(IQueryable<TSource> queryable);
}

public interface ISpecification<T> : ISpecification<T, T>;
