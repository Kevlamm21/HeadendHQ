namespace HeadendHQ.Core.Shared;

public class AllSpecification<T> : ISpecification<T>
{
	private AllSpecification() { }

	public IQueryable<T> Apply(IQueryable<T> queryable) => queryable;

	public static AllSpecification<T> Instance { get; } = new AllSpecification<T>();
}
