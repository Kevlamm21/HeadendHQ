namespace HeadendHQ.Core.Shared;

public interface ISpecification<TSource, TResult>
{
    IQueryable<TResult> Apply(IQueryable<TSource> queryable);
}

public interface ISpecification<T1, T2, TResult>
{
    IQueryable<TResult> Apply(IQueryable<T1> queryable1, IQueryable<T2> queryable2);
}

public interface ISpecification<T> : ISpecification<T, T>;
