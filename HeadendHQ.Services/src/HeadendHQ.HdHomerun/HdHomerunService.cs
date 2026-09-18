using System.Text.Json;
using System.Text.Json.Serialization;
using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Iptv;
using HeadendHQ.Core.Shared;
using HeadendHQ.HdHomerun.Settings;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.HdHomerun;

public class HdHomerunService(
    HttpClient httpClient,
    IWorkspace workspace,
    IMediator mediator,
    ILogger<HdHomerunService> logger) : IIptvService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RefreshGuideAsync(CancellationToken cancellationToken = default)
    {
        var config = await mediator.Send(new GetHdHomerunSettingsQuery(), cancellationToken);

        if (string.IsNullOrWhiteSpace(config.DeviceUrl))
        {
            logger.LogError("HDHomeRun DeviceUrl is not configured. Set it via PATCH /settings/iptv.");
            return;
        }

        logger.LogInformation("Fetching HDHomeRun device info from {Url}", config.DeviceUrl);

        var discoverJson = await httpClient.GetStringAsync(config.DeviceUrl, cancellationToken);
        using var doc = JsonDocument.Parse(discoverJson);

        if (!doc.RootElement.TryGetProperty("DeviceAuth", out var deviceAuthElement))
        {
            logger.LogError("DeviceAuth field not found in discover.json response.");
            return;
        }

        var deviceAuth = deviceAuthElement.GetString()!;
        var guideUrl = $"https://api.hdhomerun.com/api/xmltv?DeviceAuth={Uri.EscapeDataString(deviceAuth)}";

        logger.LogInformation("Fetching XMLTV programme guide from the HDHomeRun API.");

        var content = await httpClient.GetStringAsync(guideUrl, cancellationToken);

        var existing = await workspace.LoadSingleOrDefault(AllSpecification<IptvGuideCache>.Instance, cancellationToken);
        if (existing is not null)
            workspace.Remove(existing);

        workspace.Add(new IptvGuideCache { Content = content });

        logger.LogInformation("IPTV programme guide updated.");
    }

    public async Task<string?> GetGuideContentAsync(CancellationToken cancellationToken = default)
    {
        var cache = await workspace.LoadSingleOrDefault(AllSpecification<IptvGuideCache>.Instance, cancellationToken);
        return cache?.Content;
    }

    public async Task RefreshLineupAsync(CancellationToken cancellationToken = default)
    {
        var config = await mediator.Send(new GetHdHomerunSettingsQuery(), cancellationToken);

        if (string.IsNullOrWhiteSpace(config.DeviceUrl))
        {
            logger.LogError("HDHomeRun DeviceUrl is not configured. Set it via PATCH /settings/iptv.");
            return;
        }

        var lineupUrl = new Uri(new Uri(config.DeviceUrl), "lineup.json").ToString();

        List<HdHomerunLineupEntry>? entries;
        try
        {
            var json = await httpClient.GetStringAsync(lineupUrl, cancellationToken);
            entries = JsonSerializer.Deserialize<List<HdHomerunLineupEntry>>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Could not read the HDHomeRun lineup from {Url}; will retry next run.", lineupUrl);
            return;
        }

        if (entries is null || entries.Count == 0)
        {
            logger.LogWarning("HDHomeRun lineup at {Url} was empty.", lineupUrl);
            return;
        }

        var existing = (await workspace.Load(AllSpecification<IptvChannel>.Instance, cancellationToken))
            .ToDictionary(c => c.GuideNumber, StringComparer.OrdinalIgnoreCase);

        var upserted = 0;
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.GuideNumber))
                continue;

            var callSign = CallSign.Normalize(entry.GuideName);

            if (existing.TryGetValue(entry.GuideNumber, out var row))
                row.Update(callSign, entry.GuideName, null);
            else
                workspace.Add(new IptvChannel(entry.GuideNumber, callSign, entry.GuideName, null));

            upserted++;
        }

        logger.LogInformation("IPTV lineup refreshed: {Count} channel(s).", upserted);
    }

    public async Task<IReadOnlyList<IptvChannel>> GetLineupAsync(CancellationToken cancellationToken = default)
    {
        var channels = await workspace.Load(AllSpecification<IptvChannel>.Instance, cancellationToken);
        return [.. channels.OrderBy(c => c.GuideNumber, StringComparer.OrdinalIgnoreCase)];
    }

    private sealed record HdHomerunLineupEntry(
        [property: JsonPropertyName("GuideNumber")] string? GuideNumber,
        [property: JsonPropertyName("GuideName")] string? GuideName);
}
