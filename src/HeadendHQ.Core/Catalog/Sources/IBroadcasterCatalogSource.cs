namespace HeadendHQ.Core.Catalog.Sources;

public interface IBroadcasterCatalogSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<string>> ListBroadcasterIdsAsync(CancellationToken ct);

    Task<BroadcasterDescriptor?> GetBroadcasterAsync(string externalId, CancellationToken ct);
}
