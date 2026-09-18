using System.Text.Json;
using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog;
using HeadendHQ.WebScraping.Espn.Models;
using HeadendHQ.WebScraping.Espn.Transport;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Catalog;

internal sealed class EspnScheduleSource(
    EspnTransport transport,
    ILogger<EspnScheduleSource> logger) : IScheduleSource
{
    private const int PageSize = 50;

    public string SourceKey => SourceKeys.Espn;

    public async Task<IReadOnlyList<ScheduledEventDescriptor>> GetEventsAsync(
        ScheduleQuery query, CancellationToken ct)
    {
        var leagues = query.LeagueSlugs.Count > 0 ? string.Join(",", query.LeagueSlugs) : null;
        const string? watch = null;

        var events = new List<ScheduledEventDescriptor>();
        var seen = new HashSet<string>();

        for (var date = query.From; date <= query.To; date = date.AddDays(1))
        {
            foreach (var espnEvent in await FetchDayAsync(date, leagues, watch, ct))
            {
                if (!seen.Add(espnEvent.Id))
                    continue;

                if (Convert(espnEvent) is { } descriptor)
                    events.Add(descriptor);
            }
        }

        logger.LogInformation(
            "ESPN schedule: {Count} event(s) between {From} and {To}.", events.Count, query.From, query.To);

        return events;
    }

    private async Task<List<EspnEvent>> FetchDayAsync(
        DateOnly date, string? leagues, string? watch, CancellationToken ct)
    {
        var results = new List<EspnEvent>();
        var stamp = date.ToString("yyyyMMdd");

        for (var page = 1; ; page++)
        {
            ct.ThrowIfCancellationRequested();

            var json = await transport.GetStringAsync(
                EspnEndpoints.GuideFeed(stamp, page, PageSize, leagues, watch), ct);

            var root = JsonSerializer.Deserialize<EspnFeedRoot>(json);
            if (root is null)
                break;

            var events = root.Events ?? [];
            results.AddRange(events);

            if (events.Count == 0 || page * PageSize >= root.Total)
                break;
        }

        return results;
    }

    private static ScheduledEventDescriptor? Convert(EspnEvent espnEvent)
    {
        if (!DateTimeOffset.TryParse(espnEvent.Date, out var start))
            return null;

        var competitors = espnEvent.Competitors
            .Select(c => new CompetitorDescriptor(
                c.Team.Id,
                c.Team.DisplayName,
                IsHome: string.Equals(c.HomeAway, "home", StringComparison.OrdinalIgnoreCase),
                Logos: ToCandidates(c.Team.Logos)))
            .ToList();

        if (competitors.Count == 0)
            return null;

        var broadcasts = (espnEvent.Watch?.Broadcasts ?? [])
            .Where(b => !string.IsNullOrEmpty(b.Media.Slug))
            .Select(b => new BroadcastCandidate(
                ExternalId: b.Media.Id ?? b.Media.Slug!,
                Slug: b.Media.Slug!,
                Name: b.Media.Name ?? b.Media.Slug!,
                Kind: ToKind(b.Type?.Slug),
                Priority: b.Priority ?? 0,
                Market: b.Market?.Type))
            .DistinctBy(b => b.Slug)
            .ToList();

        var watchUrl = espnEvent.Watch?.Style?.Action is "paywall" or "picker"
            ? espnEvent.Watch.Style.Link
            : null;

        return new ScheduledEventDescriptor(
            ExternalId: espnEvent.Id,
            SportSlug: espnEvent.Sport.Slug,
            LeagueSlug: espnEvent.League.Slug,
            LeagueExternalId: espnEvent.League.Id,
            StartUtc: start.UtcDateTime,
            Name: espnEvent.Name,
            Competitors: competitors,
            Broadcasts: broadcasts,
            WatchUrl: watchUrl,
            SeasonYear: espnEvent.Season?.Year,
            SeasonType: espnEvent.Season?.Type);
    }

    private static BroadcasterKind ToKind(string? slug) => slug switch
    {
        "television" => BroadcasterKind.Television,
        "streaming" => BroadcasterKind.Streaming,
        _ => BroadcasterKind.Unknown,
    };

    private static IReadOnlyList<ImageCandidate>? ToCandidates(List<EspnLogo>? logos) =>
        logos is null ? null
        : [.. logos
            .Where(l => !string.IsNullOrEmpty(l.Href))
            .Select(l => new ImageCandidate(
                LogoRels.Normalize(l.Rel ?? []), l.Href, l.Width, l.Height, l.LastUpdated))];
}
