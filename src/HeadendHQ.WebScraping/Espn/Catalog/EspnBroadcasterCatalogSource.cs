using System.Text.Json;
using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.WebScraping.Espn.Models;
using HeadendHQ.WebScraping.Espn.Transport;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Catalog;

internal sealed class EspnBroadcasterCatalogSource(
    EspnTransport transport,
    ILogger<EspnBroadcasterCatalogSource> logger) : IBroadcasterCatalogSource
{
    public string SourceKey => SourceKeys.Espn;

    public async Task<IReadOnlyList<string>> ListBroadcasterIdsAsync(CancellationToken ct)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>();

        for (var page = 1; ; page++)
        {
            var json = await transport.GetStringAsync(EspnEndpoints.MediaIndex(page), ct);
            var list = JsonSerializer.Deserialize<EspnRefList>(json);

            foreach (var item in list?.Items ?? [])
            {
                if (string.IsNullOrEmpty(item.Ref))
                    continue;

                var id = EspnRefs.LastSegment(item.Ref);
                if (id.Length > 0 && id.All(char.IsDigit) && seen.Add(id))
                    ids.Add(id);
            }

            if (list is null || list.Items is null or { Count: 0 } || page >= list.PageCount)
                break;
        }

        logger.LogInformation("ESPN media index: {Count} broadcaster id(s).", ids.Count);
        return ids;
    }

    public async Task<BroadcasterDescriptor?> GetBroadcasterAsync(string externalId, CancellationToken ct)
    {
        try
        {
            var json = await transport.GetStringAsync(EspnEndpoints.Media(externalId), ct);
            var media = JsonSerializer.Deserialize<EspnMediaDetail>(json);

            if (media?.Slug is null)
                return null;

            return new BroadcasterDescriptor(
                ExternalId: media.Id ?? externalId,
                Slug: media.Slug,
                Name: media.Name ?? media.Slug,
                ShortName: media.ShortName,
                CallLetters: media.CallLetters,
                Logos: [.. (media.Logos ?? [])
                    .Where(l => !string.IsNullOrEmpty(l.Href))
                    .Select(l => new ImageCandidate(
                        LogoRels.Normalize(l.Rel ?? []), l.Href, l.Width, l.Height, l.LastUpdated))]);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not EspnThrottledException)
        {
            logger.LogWarning(ex, "Failed to resolve ESPN media {MediaId}.", externalId);
            return null;
        }
    }
}
