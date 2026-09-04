using System.Security.Cryptography;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Media;

public enum ImageOrigin
{
    /// <summary>Downloaded from a catalog source such as ESPN.</summary>
    Fetched,

    /// <summary>Uploaded by the user. Never overwritten by a refresh.</summary>
    Manual
}

/// <summary>
/// The bytes of a single image, stored once and shared by every asset that points at it.
/// Identity is the content hash, so re-fetching an unchanged logo is a no-op and two teams
/// sharing artwork share a row.
/// <para>
/// This is also the only place upstream state lives. Assets hold a plain <c>ImageId</c>, so the
/// address the bytes came from and the validators needed to skip the next download are recorded
/// here once rather than copied onto every row that points at them.
/// </para>
/// </summary>
public class Image : IEntity<int>
{
    private Image() { }

    private Image(
        byte[] bytes, string sha256, string contentType, int width, int height,
        ImageOrigin origin, ImagePurpose purpose, string? sourceUrl, int? leagueId,
        string? etag, DateTimeOffset? lastModifiedUtc)
    {
        Bytes = bytes;
        Sha256 = sha256;
        ContentType = contentType;
        Width = width;
        Height = height;
        Origin = origin;
        Purpose = purpose;
        SourceUrl = sourceUrl;
        LeagueId = leagueId;
        ETag = etag;
        LastModifiedUtc = lastModifiedUtc;
        FetchedAtUtc = sourceUrl is null ? null : DateTimeOffset.UtcNow;
    }

    public int Id { get; init; }
    public string Sha256 { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "image/png";
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte[] Bytes { get; private set; } = [];
    public ImageOrigin Origin { get; private set; }

    /// <summary>
    /// What these bytes are for. Recorded rather than inferred so a class of image can be swept in
    /// one query. It also decides how the bytes were normalized on the way in, which is why it is
    /// half of this row's upstream identity: the same URL fetched as a headshot and as a team logo
    /// produces different pixels and therefore different rows.
    /// </summary>
    public ImagePurpose Purpose { get; private set; }

    /// <summary>
    /// Where the bytes came from, checked <em>before</em> a download. Content addressing only
    /// dedupes after the fact, so this is what makes a player's face cost one request ever rather
    /// than one per game. Null for hand uploads, which have no upstream, and for a row that has been
    /// superseded by newer bytes at the same address.
    /// </summary>
    public string? SourceUrl { get; private set; }

    /// <summary>
    /// The league the image belongs to, when the caller knows one. Scopes a headshot wipe to the
    /// league that just had its media day.
    /// </summary>
    public int? LeagueId { get; private set; }

    /// <summary>
    /// Validators from the last fetch, so a refresh can send a conditional request and settle for a
    /// 304. Kept here rather than on the assets pointing at this row: they describe the bytes, and
    /// two teams sharing a logo would otherwise each hold their own copy of the same ETag.
    /// </summary>
    public string? ETag { get; private set; }

    public DateTimeOffset? LastModifiedUtc { get; private set; }

    /// <summary>When the source last confirmed these bytes are current, by any means.</summary>
    public DateTimeOffset? FetchedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Image Create(
        byte[] bytes, string contentType, int width, int height, ImageOrigin origin,
        ImagePurpose purpose, string? sourceUrl = null, int? leagueId = null,
        string? etag = null, DateTimeOffset? lastModifiedUtc = null) =>
        new(bytes, ComputeHash(bytes), contentType, width, height, origin, purpose, sourceUrl,
            leagueId, etag, lastModifiedUtc);

    /// <summary>The source says these bytes are still current. Refreshes the validators, not the bytes.</summary>
    public void Revalidated(string? etag = null, DateTimeOffset? lastModifiedUtc = null)
    {
        if (etag is not null) ETag = etag;
        if (lastModifiedUtc is not null) LastModifiedUtc = lastModifiedUtc;
        FetchedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Releases this row's claim on its address, because newer bytes now live there.
    /// <para>
    /// The bytes are deliberately left alone. <c>/media/images/{id}</c> promises that what is served
    /// at an id never changes — Jellyfin caches it as immutable — so a rebrand becomes a new row and
    /// this one is left for the orphan sweep rather than rewritten underneath its readers.
    /// </para>
    /// </summary>
    public void Supersede()
    {
        SourceUrl = null;
        ETag = null;
        LastModifiedUtc = null;
    }

    /// <summary>
    /// Claims an address for bytes we already hold. Happens when a re-download hashes to a row that
    /// had no upstream of its own — without it the same URL would be re-fetched on every refresh.
    /// </summary>
    public void AdoptSource(string sourceUrl, string? etag, DateTimeOffset? lastModifiedUtc)
    {
        SourceUrl = sourceUrl;
        ETag = etag;
        LastModifiedUtc = lastModifiedUtc;
        FetchedAtUtc = DateTimeOffset.UtcNow;
    }

    public static string ComputeHash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
