using HeadendHQ.Core.Catalog.Broadcasters;

namespace HeadendHQ.Core.Catalog.Sources;

public record ImageCandidate(string Rel, string Url, int? Width = null, int? Height = null, DateTimeOffset? UpdatedAtUtc = null);

public record SportDescriptor(string ExternalId, string Slug, string Name);

public record LeagueDescriptor(
    string ExternalId,
    string Slug,
    string Name,
    string? Abbreviation = null,
    string? ShortName = null,
    bool SupportsTeams = true,
    IReadOnlyList<ImageCandidate>? Logos = null);

public record TeamDescriptor(
    string ExternalId,
    string DisplayName,
    string? ShortDisplayName = null,
    string? Slug = null,
    string? Abbreviation = null,
    string? Location = null,
    string? Nickname = null,
    string? PrimaryColorHex = null,
    string? AlternateColorHex = null,
    bool IsActive = true,
    IReadOnlyList<ImageCandidate>? Logos = null);

public record AthleteDescriptor(
    string ExternalId,
    string DisplayName,
    string? ShortName = null,
    string? Position = null,
    string? Jersey = null,
    int? ExperienceYears = null,
    string? HeadshotUrl = null,
    string? TeamExternalId = null);

public record BroadcasterDescriptor(
    string ExternalId,
    string Slug,
    string Name,
    string? ShortName = null,
    string? CallLetters = null,
    BroadcasterKind Kind = BroadcasterKind.Unknown,
    IReadOnlyList<ImageCandidate>? Logos = null);

public record BroadcastCandidate(
    string ExternalId,
    string Slug,
    string Name,
    BroadcasterKind Kind,
    int Priority = 0,
    string? Market = null);

public record CompetitorDescriptor(
    string? TeamExternalId,
    string DisplayName,
    bool IsHome,
    IReadOnlyList<ImageCandidate>? Logos = null);

public record ScheduledEventDescriptor(
    string ExternalId,
    string SportSlug,
    string LeagueSlug,
    string? LeagueExternalId,
    DateTime StartUtc,
    string? Name,
    IReadOnlyList<CompetitorDescriptor> Competitors,
    IReadOnlyList<BroadcastCandidate> Broadcasts,
    string? WatchUrl = null,
    int? SeasonYear = null,
    int? SeasonType = null);

public record EventDetailDescriptor(
    string? VenueName = null,
    string? Note = null,
    string? SeriesType = null,
    string? SeriesSummary = null,
    int? SeasonYear = null,
    int? SeasonType = null,
    IReadOnlyList<CastCandidate>? Cast = null);

public record CastCandidate(
    AthleteDescriptor Athlete,
    bool IsHome,
    string TeamDisplayName,
    string? TeamExternalId = null,
    bool IsListedStarter = false,
    bool IsStatLeader = false,
    bool IsProbableStarter = false,
    int? DepthRank = null,
    string? InjuryStatus = null);

public record ScheduleQuery(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<string> LeagueSlugs,
    IReadOnlyList<string> BroadcasterSlugs);

public record LeagueKey(string SportSlug, string LeagueSlug, string? LeagueExternalId = null);

public record TeamKey(LeagueKey League, string TeamExternalId);

public record EventKey(string SportSlug, string LeagueSlug, string EventExternalId);
