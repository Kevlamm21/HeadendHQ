namespace HeadendHQ.Core.Catalog.Sources;

public record FetchedImage(byte[] Bytes, string ContentType, string? ETag, DateTimeOffset? LastModifiedUtc);

public interface IImageFetcher
{
    Task<FetchedImage?> FetchAsync(string url, string? etag, DateTimeOffset? lastModifiedUtc, CancellationToken ct);
}
