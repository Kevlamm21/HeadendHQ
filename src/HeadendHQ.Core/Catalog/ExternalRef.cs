namespace HeadendHQ.Core.Catalog;

/// <summary>
/// How a catalog source identifies this entity, held as flat columns on the entity's own table.
/// A row is sourced from exactly one place, so keeping the pair on the main table makes "find the
/// team ESPN calls 12" a single indexed comparison with no join. A second source replaces the pair
/// rather than sitting beside it.
/// </summary>
public interface IExternalRef
{
    string? SourceKey { get; }
    string? ExternalId { get; }
}

public static class ExternalRefExtensions
{
    /// <summary>
    /// The id <paramref name="sourceKey"/> knows this entity by, or null when the entity was
    /// sourced elsewhere — so a swapped source never silently reuses the old source's ids.
    /// </summary>
    public static string? ExternalIdFor(this IExternalRef entity, string sourceKey) =>
        entity.SourceKey == sourceKey ? entity.ExternalId : null;
}
