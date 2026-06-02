namespace HeadendHQ.Core;

public interface IUnitOfWork
{
    Task SaveChanges(CancellationToken ct = default);
}
