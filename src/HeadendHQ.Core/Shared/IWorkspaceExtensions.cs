namespace HeadendHQ.Core.Shared;

public static class IWorkspaceExtensions
{
	public static async Task<IReadOnlyList<T>> LoadAll<T>(this IWorkspace workspace, CancellationToken cancellationToken)
		where T : class
	{
		var all = await workspace.Load(AllSpecification<T>.Instance, cancellationToken);
		return all;
	}

	public static async Task<TEntity?> LoadSingleOrDefault<TEntity>(this IWorkspace workspace, ISpecification<TEntity> spec, CancellationToken cancellationToken)
		where TEntity : class
	{
		var results = await workspace.Load(spec, cancellationToken);
		if (results.Count == 1)
		{
			return results[0];
		}

		return default;
	}

	public static async Task<TEntity> LoadById<TEntity, TId>(this IWorkspace workspace, TId id, CancellationToken cancellationToken)
		where TEntity : class, IEntity<TId>
	{
		var spec = new EntityByIdSpecification<TEntity, TId>(id);
		var result = await workspace.LoadSingleOrDefault(spec, cancellationToken);
		if (result is null)
		{
			throw new NotFoundException<TEntity>(id?.ToString() ?? string.Empty);
		}

		return result;
	}
}
