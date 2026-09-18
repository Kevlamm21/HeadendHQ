namespace HeadendHQ.Core.Shared;

public interface IUnitOfWork
{
    Task SaveChanges(CancellationToken ct = default);
}
