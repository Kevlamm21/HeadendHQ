namespace HeadendHQ.Core.Catalog;

/// <summary>
/// How one catalog source identifies this entity. Held as a collection so an entity can keep its
/// ESPN id alongside a second source's id, which is what makes swapping the source survivable.
/// </summary>
public class ExternalRef
{
    private ExternalRef() { }

    public ExternalRef(string sourceKey, string externalId)
    {
        SourceKey = sourceKey;
        ExternalId = externalId;
    }

    public string SourceKey { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;
}

public static class ExternalRefExtensions
{
    /// <summary>
    /// Records how <paramref name="sourceKey"/> identifies this entity, replacing that source's
    /// previous id. Other sources' refs are left alone.
    /// </summary>
    public static void Track(this List<ExternalRef> refs, string sourceKey, string externalId)
    {
        if (refs.Any(r => r.SourceKey == sourceKey && r.ExternalId == externalId))
            return;

        refs.RemoveAll(r => r.SourceKey == sourceKey);
        refs.Add(new ExternalRef(sourceKey, externalId));
    }

    public static string? ExternalIdFor(this List<ExternalRef> refs, string sourceKey) =>
        refs.FirstOrDefault(r => r.SourceKey == sourceKey)?.ExternalId;
}
