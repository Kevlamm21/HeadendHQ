using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Events;

/// <summary>
/// Flattens a <see cref="SportingEvent"/> into the <see cref="TitleRequest"/> used to create its
/// title — name, dates, launch slug, and the NFO-mapped fields (studio/genres/plot/tagline/uniqueid,
/// cast). Pure: the caller loads the event and its league first. A video game gets its own mapper.
/// </summary>
public static class SportingEventTitleMapper
{
    public static TitleRequest ToTitleRequest(SportingEvent ev, League league, string? launchSlug) => new()
    {
        // Home first, matching the poster: home takes the top half and the left half.
        Name = $"{ev.HomeTeamName} vs {ev.AwayTeamName}",
        Type = TitleType.SportingEvent,
        SourceId = ev.Id,
        LaunchSlug = launchSlug,
        EventUrl = ev.WatchUrl,
        StartUtc = ev.StartUtc,
        EndUtc = ev.EndUtc,

        Studio = league.Name,
        Genres = ["Sports", league.Name],
        Sets = [league.Name],
        Plot = $"{ev.AwayTeamName} at {ev.HomeTeamName}.",
        // Jellyfin has no venue tag, so the venue rides along on the tagline.
        Tagline = CombineTagline(ev.Note ?? $"{league.Name} {SeasonLabel(ev.SeasonType)}", ev.VenueName),
        UniqueId = $"{ev.SourceKey.ToLowerInvariant()}:{ev.ExternalId}",
        Cast =
        [
            .. ev.Cast
                .OrderBy(c => c.Order)
                .Select(c => new TitleCastEntry { Name = c.Name, Role = c.Role, HeadshotImageId = c.HeadshotImageId })
        ],
    };

    private static string SeasonLabel(int? seasonType) => seasonType switch
    {
        1 => "Preseason",
        3 => "Postseason",
        _ => "Regular Season",
    };

    private static string CombineTagline(string line, string? venue) =>
        string.IsNullOrWhiteSpace(venue) ? line : $"{line} — {venue}";
}
