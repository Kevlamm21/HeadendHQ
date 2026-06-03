using System.Linq.Expressions;

namespace HeadendHQ.Core.Shared;

public static class IReadModelExtensions
{
	public static async Task<IReadOnlyList<T>> All<T>(this IReadModel readModel, CancellationToken cancellationToken)
		where T : class
	{
		var results = await readModel.Search(AllSpecification<T>.Instance, cancellationToken);
		return results;
	}

	public static async Task<TResult?> SingleOrDefault<TEntity, TResult>(this IReadModel readModel, ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken)
		where TEntity : class
	{
		var results = await readModel.Search(spec, cancellationToken);
		if (results.Count == 1)
		{
			return results[0];
		}

		return default;
	}

	public static IOrderedQueryable<T> SortBy<T, TValue>(this IQueryable<T> queryable, Expression<Func<T, TValue>> sortBy, SortDirection? sortDirection = null)
	{
		if (sortDirection.HasValue && sortDirection.Value == SortDirection.Desc)
		{
			return queryable.OrderByDescending(sortBy);
		}
		else
		{
			return queryable.OrderBy(sortBy);
		}
	}

	public static IOrderedQueryable<T> ThenSortBy<T, TValue>(this IOrderedQueryable<T> queryable, Expression<Func<T, TValue>> sortBy, SortDirection? sortDirection = null)
	{
		if (sortDirection.HasValue && sortDirection.Value == SortDirection.Desc)
		{
			return queryable.ThenByDescending(sortBy);
		}
		else
		{
			return queryable.ThenBy(sortBy);
		}
	}
}

public enum SortDirection
{
	Asc,
	Desc
}