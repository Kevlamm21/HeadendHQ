using System.Security.Cryptography;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Media;

public enum ImageOrigin
{
    Fetched,

    Manual
}

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

    public ImagePurpose Purpose { get; private set; }

    public string? SourceUrl { get; private set; }

    public int? LeagueId { get; private set; }

    public string? ETag { get; private set; }

    public DateTimeOffset? LastModifiedUtc { get; private set; }

    public DateTimeOffset? FetchedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Image Create(
        byte[] bytes, string contentType, int width, int height, ImageOrigin origin,
        ImagePurpose purpose, string? sourceUrl = null, int? leagueId = null,
        string? etag = null, DateTimeOffset? lastModifiedUtc = null) =>
        new(bytes, ComputeHash(bytes), contentType, width, height, origin, purpose, sourceUrl,
            leagueId, etag, lastModifiedUtc);

    public void Revalidated(string? etag = null, DateTimeOffset? lastModifiedUtc = null)
    {
        if (etag is not null) ETag = etag;
        if (lastModifiedUtc is not null) LastModifiedUtc = lastModifiedUtc;
        FetchedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Supersede()
    {
        SourceUrl = null;
        ETag = null;
        LastModifiedUtc = null;
    }

    public void AdoptSource(string sourceUrl, string? etag, DateTimeOffset? lastModifiedUtc)
    {
        SourceUrl = sourceUrl;
        ETag = etag;
        LastModifiedUtc = lastModifiedUtc;
        FetchedAtUtc = DateTimeOffset.UtcNow;
    }

    public static string ComputeHash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
