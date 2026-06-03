namespace HeadendHQ.Core.Shared;

public class EntityByIdSpecification<T, TId>(TId Id) : ISpecification<T>
	where T : IEntity<TId>
{
	public IQueryable<T> Apply(IQueryable<T> queryable) => queryable.Where(entity => entity.Id!.Equals(Id));
}
