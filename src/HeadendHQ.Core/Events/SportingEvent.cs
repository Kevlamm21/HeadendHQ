using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Events;

/// <summary>
/// A scheduled game, as the schedule source sees it.
/// <para>
/// This is the sports domain in full: which league, which teams, where it can be watched, who is
/// billed. It is deliberately separate from <see cref="Titles.Title"/> — a title is the flattened,
/// type-agnostic artifact we produce for Jellyfin, and keeping the two apart means a video game or a
/// movie can grow its own equally rich entity and map into the same title without the NFO and
/// artwork writers learning anything about sports.
/// </para>
/// <para>
/// Events are scraped for the whole window; a title is produced only when the game is due.
/// </para>
/// </summary>
public class SportingEvent : Entity<Guid>
{
    private SportingEvent() { }

    public SportingEvent(string sourceKey, string externalId, int leagueId, DateTime startUtc)
    {
        SourceKey = sourceKey;
        ExternalId = externalId;
        LeagueId = leagueId;
        StartUtc = startUtc;
        EndUtc = startUtc.AddHours(3);
    }

    public override Guid Id { get; init; } = Guid.NewGuid();

    public string SourceKey { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;

    public int LeagueId { get; private set; }
    public int? HomeTeamId { get; private set; }
    public int? AwayTeamId { get; private set; }
    public int? BroadcasterId { get; private set; }

    /// <summary>Kept alongside the ids so an event still reads correctly if a team is later removed.</summary>
    public string HomeTeamName { get; private set; } = string.Empty;
    public string AwayTeamName { get; private set; } = string.Empty;

    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }

    /// <summary>Which edition of the league this belongs to. See <see cref="LogoVariants"/>.</summary>
    public string Variant { get; private set; } = LogoVariants.Default;

    public int? SeasonYear { get; private set; }

    /// <summary>Source season type: 1 preseason, 2 regular, 3 postseason, 4 off-season.</summary>
    public int? SeasonType { get; private set; }

    public string? VenueName { get; private set; }

    /// <summary>The competition note, e.g. "NBA Cup - Semifinals". Also what decides the variant.</summary>
    public string? Note { get; private set; }

    public string? SeriesType { get; private set; }
    public string? SeriesSummary { get; private set; }

    /// <summary>Where the event can be watched, when the source offers a usable link.</summary>
    public string? WatchUrl { get; private set; }

    /// <summary>Set once detail collection has run, so it never repeats for the same event.</summary>
    public DateTimeOffset? DetailsFetchedAtUtc { get; private set; }

    /// <summary>The title produced from this event, once it has been produced.</summary>
    public Guid? TitleId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    public List<SportingEventCastMember> Cast { get; private set; } = [];

    public bool NeedsDetails => DetailsFetchedAtUtc is null;
    public bool NeedsTitle => TitleId is null;

    public void SetSchedule(DateTime startUtc, DateTime? endUtc)
    {
        StartUtc = startUtc;
        EndUtc = endUtc ?? startUtc.AddHours(3);
        Touch();
    }

    public void SetParticipants(
        int? homeTeamId, string homeTeamName, int? awayTeamId, string awayTeamName, int? broadcasterId)
    {
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        HomeTeamName = homeTeamName;
        AwayTeamName = awayTeamName;
        BroadcasterId = broadcasterId;
        Touch();
    }

    public void SetSeason(int? seasonYear, int? seasonType)
    {
        if (seasonYear is not null) SeasonYear = seasonYear;
        if (seasonType is not null) SeasonType = seasonType;
        Touch();
    }

    public void SetWatchUrl(string? watchUrl)
    {
        WatchUrl = string.IsNullOrWhiteSpace(watchUrl) ? null : watchUrl;
        Touch();
    }

    public void SetVariant(string variant)
    {
        Variant = variant;
        Touch();
    }

    /// <summary>
    /// Applies the second, per-event lookup. Recorded even when the source returned nothing, so a
    /// game with no venue on file is not re-requested every night.
    /// </summary>
    public void ApplyDetails(
        string? venueName, string? note, string? seriesType, string? seriesSummary, string variant)
    {
        VenueName = venueName;
        Note = note;
        SeriesType = seriesType;
        SeriesSummary = seriesSummary;
        Variant = variant;
        DetailsFetchedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void SetCast(IEnumerable<int> athleteIdsInBillingOrder)
    {
        Cast.Clear();
        var order = 0;

        foreach (var athleteId in athleteIdsInBillingOrder)
            Cast.Add(new SportingEventCastMember(athleteId, order++));

        Touch();
    }

    public void AttachTitle(Guid titleId)
    {
        TitleId = titleId;
        Touch();
    }

    /// <summary>The title was deleted or expired; the event may be produced again if still relevant.</summary>
    public void DetachTitle()
    {
        TitleId = null;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
