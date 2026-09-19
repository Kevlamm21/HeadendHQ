using HeadendHQ.Core.Catalog.Broadcasters;

namespace HeadendHQ.Core;

public interface IBroadcasterCatalogSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<string>> ListBroadcasterIdsAsync(CancellationToken ct);

    Task<BroadcasterRequest?> GetBroadcasterAsync(string externalId, CancellationToken ct);
}
