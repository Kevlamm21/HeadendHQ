using System.Security.Cryptography;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Media;

public enum ImageOrigin
{
    /// <summary>Downloaded from a catalog source such as ESPN.</summary>
    Fetched,

    /// <summary>Uploaded by the user. Never overwritten by a refresh.</summary>
    Manual,

    /// <summary>Composed by us (posters, backgrounds).</summary>
    Generated
}

/// <summary>
/// The bytes of a single image, stored once and shared by every asset that points at it.
/// Identity is the content hash, so re-fetching an unchanged logo is a no-op and two teams
/// sharing artwork share a row.
/// </summary>
public class Image : IEntity<int>
{
    private Image() { }

    private Image(byte[] bytes, string sha256, string contentType, int width, int height, ImageOrigin origin)
    {
        Bytes = bytes;
        Sha256 = sha256;
        ContentType = contentType;
        Width = width;
        Height = height;
        Origin = origin;
    }

    public int Id { get; init; }
    public string Sha256 { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "image/png";
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte[] Bytes { get; private set; } = [];
    public ImageOrigin Origin { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Image Create(byte[] bytes, string contentType, int width, int height, ImageOrigin origin) =>
        new(bytes, ComputeHash(bytes), contentType, width, height, origin);

    public static string ComputeHash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
