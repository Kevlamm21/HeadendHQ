namespace HeadendHQ.Core.Catalog.Sources;

/// <summary>Bytes plus the validators needed to skip the next download.</summary>
public record FetchedImage(byte[] Bytes, string ContentType, string? ETag, DateTimeOffset? LastModifiedUtc);

public interface IImageFetcher
{
    /// <summary>
    /// Downloads an image, sending conditional headers when validators are supplied.
    /// Returns <c>null</c> when the server answers 304, meaning the bytes we hold are still current.
    /// </summary>
    Task<FetchedImage?> FetchAsync(string url, string? etag, DateTimeOffset? lastModifiedUtc, CancellationToken ct);
}
