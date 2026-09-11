using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Events;

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

    public string HomeTeamName { get; private set; } = string.Empty;
    public string AwayTeamName { get; private set; } = string.Empty;

    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }

    public string Variant { get; private set; } = LogoVariants.Default;

    public int? SeasonYear { get; private set; }

    public int? SeasonType { get; private set; }

    public string? VenueName { get; private set; }

    public string? Note { get; private set; }

    public string? SeriesType { get; private set; }
    public string? SeriesSummary { get; private set; }

    public string? WatchUrl { get; private set; }

    public DateTimeOffset? DetailsFetchedAtUtc { get; private set; }

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

    public void ApplyDetail(EventDetail detail)
    {
        var source = detail.Source;

        SetCast(detail.Cast);
        SetSeason(source?.SeasonYear, source?.SeasonType);
        ApplyDetails(
            source?.VenueName, source?.Note, source?.SeriesType, source?.SeriesSummary,
            LeagueVariantResolver.Resolve(source?.Note, SeasonType));
    }

    public void RefreshVariant() => SetVariant(LeagueVariantResolver.Resolve(Note, SeasonType));

    public void SetCast(IEnumerable<BilledAthlete> billedInOrder)
    {
        Cast.Clear();
        var order = 0;

        foreach (var billed in billedInOrder)
            Cast.Add(new SportingEventCastMember(
                billed.Name, billed.Role, billed.HeadshotImageId, order++));

        Touch();
    }

    public void ResetDetails()
    {
        DetailsFetchedAtUtc = null;
        Touch();
    }

    public void AttachTitle(Guid titleId)
    {
        TitleId = titleId;
        Touch();
    }

    public void DetachTitle()
    {
        TitleId = null;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
