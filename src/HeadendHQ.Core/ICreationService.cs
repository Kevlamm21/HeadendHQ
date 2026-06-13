namespace HeadendHQ.Core;

public interface ICreationService
{
    Task CreateForTitleAsync(Guid titleId, CancellationToken ct = default);
}
