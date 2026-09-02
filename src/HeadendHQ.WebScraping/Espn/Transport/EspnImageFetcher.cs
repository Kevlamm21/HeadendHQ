using HeadendHQ.Core.Catalog.Sources;

namespace HeadendHQ.WebScraping.Espn.Transport;

/// <summary>
/// Downloads logos and headshots through the same paced, correctly-headered transport as JSON.
/// <para>
/// Conditional headers are the point: a nightly refresh of a league's team logos costs a handful of
/// 304s and no bytes at all unless something genuinely changed.
/// </para>
/// </summary>
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
