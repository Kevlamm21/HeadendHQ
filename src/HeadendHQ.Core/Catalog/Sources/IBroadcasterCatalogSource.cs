namespace HeadendHQ.Core.Catalog.Sources;

/// <summary>
/// Reads broadcaster reference data (name, call letters, logos) from a catalog source.
/// <para>
/// The ids are cheap and the records are not. <see cref="ListBroadcasterIdsAsync"/> pages ESPN's
/// media index — two requests for ~1309 ids, because the index is nothing but links whose trailing
/// segment is the id. <see cref="GetBroadcasterAsync"/> then costs one request per record. A
/// full pre-seed crawl runs once as a background job; individual sightings from the schedule resolve
/// one record at a time.
/// </para>
/// </summary>
public interface IBroadcasterCatalogSource
{
    string SourceKey { get; }

    /// <summary>Every broadcaster id the source knows, cheaply — paged links, no records fetched.</summary>
    Task<IReadOnlyList<string>> ListBroadcasterIdsAsync(CancellationToken ct);

    /// <summary>The full record for one broadcaster id, or <c>null</c> when the source has none.</summary>
    Task<BroadcasterDescriptor?> GetBroadcasterAsync(string externalId, CancellationToken ct);
}
