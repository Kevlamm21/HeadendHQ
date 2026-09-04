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
/// </summary>
public class Image : IEntity<int>
{
    private Image() { }

    private Image(
        byte[] bytes, string sha256, string contentType, int width, int height,
        ImageOrigin origin, ImagePurpose purpose, string? sourceUrl, int? leagueId)
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
    /// one query. A re-fetch that hashes to an existing row reuses it and keeps the purpose it was
    /// first stored under; in practice a headshot never hashes to a logo.
    /// </summary>
    public ImagePurpose Purpose { get; private set; }

    /// <summary>
    /// Where the bytes came from, checked <em>before</em> a download. Content addressing only
    /// dedupes after the fact, so this is what makes a player's face cost one request ever rather
    /// than one per game. Null for hand uploads, which have no upstream.
    /// </summary>
    public string? SourceUrl { get; private set; }

    /// <summary>
    /// The league the image belongs to, when the caller knows one. Scopes a headshot wipe to the
    /// league that just had its media day.
    /// </summary>
    public int? LeagueId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Image Create(
        byte[] bytes, string contentType, int width, int height, ImageOrigin origin,
        ImagePurpose purpose, string? sourceUrl = null, int? leagueId = null) =>
        new(bytes, ComputeHash(bytes), contentType, width, height, origin, purpose, sourceUrl, leagueId);

    public static string ComputeHash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
