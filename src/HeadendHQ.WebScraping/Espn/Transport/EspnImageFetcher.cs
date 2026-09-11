using HeadendHQ.Core.Catalog.Sources;

namespace HeadendHQ.WebScraping.Espn.Transport;

internal sealed class EspnImageFetcher(EspnTransport transport) : IImageFetcher
{
    public async Task<FetchedImage?> FetchAsync(
        string url, string? etag, DateTimeOffset? lastModifiedUtc, CancellationToken ct)
    {
        using var response = await transport.SendAsync(url, etag, lastModifiedUtc, ct);

        if (response is null)
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);

        return new FetchedImage(
            bytes,
            response.Content.Headers.ContentType?.MediaType ?? "image/png",
            response.Headers.ETag?.ToString(),
            response.Content.Headers.LastModified);
    }
}
