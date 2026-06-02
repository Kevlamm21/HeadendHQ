namespace HeadendHQ.Core;

public interface ICreationService
{
    Task CreateDailyAsync(CancellationToken ct = default);
}
