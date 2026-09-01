using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Media;

/// <summary>
/// A slot for an image that may not have been downloaded yet.
/// <para>
/// Catalog discovery records thousands of logo URLs but downloads only what we actually render, so
/// an <see cref="ImageRef"/> normally starts life with a <see cref="SourceUrl"/> and no
/// <see cref="ImageId"/>. A front end can point at <see cref="SourceUrl"/> in the meantime.
/// </para>
/// <para>
/// <see cref="ETag"/> and <see cref="LastModifiedUtc"/> carry the validators from the last fetch so
/// a refresh can send a conditional request and settle for a 304.
/// </para>
/// </summary>
public class ImageRef
{
    public string? SourceKey { get; private set; }
    public string? SourceUrl { get; private set; }
    public string? ETag { get; private set; }
    public DateTimeOffset? LastModifiedUtc { get; private set; }

    /// <summary>When the source last told us this URL changed, if it says so at all.</summary>
    public DateTimeOffset? SourceUpdatedAtUtc { get; private set; }

    public DateTimeOffset? FetchedAtUtc { get; private set; }
    public int? ImageId { get; private set; }

    public bool IsMaterialized => ImageId is > 0;

    public static ImageRef FromSource(string sourceKey, string sourceUrl, DateTimeOffset? sourceUpdatedAtUtc = null) =>
        new() { SourceKey = sourceKey, SourceUrl = sourceUrl, SourceUpdatedAtUtc = sourceUpdatedAtUtc };

    public static ImageRef Empty() => new();

    /// <summary>
    /// Points at a new upstream URL. Deliberately keeps <see cref="ImageId"/> so the currently
    /// rendered image stays valid until the replacement is downloaded.
    /// </summary>
    public void PointAt(string sourceKey, string sourceUrl, DateTimeOffset? sourceUpdatedAtUtc = null)
    {
        if (SourceUrl != sourceUrl)
        {
            ETag = null;
            LastModifiedUtc = null;
        }

        SourceKey = sourceKey;
        SourceUrl = sourceUrl;
        SourceUpdatedAtUtc = sourceUpdatedAtUtc;
    }

    public void Materialize(int imageId, string? etag, DateTimeOffset? lastModifiedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(imageId, 1);

        ImageId = imageId;
        ETag = etag;
        LastModifiedUtc = lastModifiedUtc;
        FetchedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>A conditional GET came back 304 — the bytes we hold are still current.</summary>
    public void MarkUnchanged() => FetchedAtUtc = DateTimeOffset.UtcNow;

    public void ClearMaterialization()
    {
        ImageId = null;
        ETag = null;
        LastModifiedUtc = null;
        FetchedAtUtc = null;
    }

    /// <summary>A hand upload replaces the slot outright and detaches it from the source URL.</summary>
    public void AttachUpload(int imageId)
    {
        ImageId = imageId;
        SourceKey = null;
        SourceUrl = null;
        ETag = null;
        LastModifiedUtc = null;
        SourceUpdatedAtUtc = null;
        FetchedAtUtc = DateTimeOffset.UtcNow;
    }
}
