using HeadendHQ.Core;

namespace HeadendHQ.WebScraping.Transport;

internal sealed class WebImageFetcher(WebTransport transport) : IImageFetcher
{
    public async Task<FetchedImage?> FetchAsync(
        string url, string? etag, DateTimeOffset? lastModifiedUtc, CancellationToken ct)
    {
        using var response = await transport.SendAsync(url, etag, lastModifiedUtc, ct);

        if (response is null)
            return null;

        return new FetchedImage(
            await response.Content.ReadAsByteArrayAsync(ct),
            response.Content.Headers.ContentType?.MediaType ?? "image/png",
            response.Headers.ETag?.ToString(),
            response.Content.Headers.LastModified);
    }
}
