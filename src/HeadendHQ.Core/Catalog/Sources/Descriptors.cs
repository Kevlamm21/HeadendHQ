using HeadendHQ.Core.Catalog.Broadcasters;

namespace HeadendHQ.Core.Catalog.Sources;

/// <summary>
/// Source-neutral shapes returned by catalog and schedule sources. Nothing ESPN-specific may appear
/// here: this file is the seam a second source would implement against.
/// </summary>

/// <summary>One candidate image, not yet downloaded.</summary>
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

/// <summary>Where a single event can be watched.</summary>
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

/// <summary>An event as the schedule source sees it, before it becomes a Title.</summary>
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

/// <summary>The extra detail a second call per event buys: venue, notes, series and cast.</summary>
public record EventDetailDescriptor(
    string? VenueName = null,
    string? Note = null,
    string? SeriesType = null,
    string? SeriesSummary = null,
    int? SeasonYear = null,
    int? SeasonType = null,
    IReadOnlyList<CastCandidate>? Cast = null);

/// <summary>
/// An athlete proposed for a title's cast, with the signals that justify billing them. Ranking is
/// the consumer's job, so a second source only has to say what it knows.
/// </summary>
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

/// <param name="BroadcasterSlugs">
/// The services the caller subscribes to. A hint about what matters, never an exclusion filter: a
/// source must still return events on broadcasters it has not been told about, or a network could
/// never be discovered — and so could never be subscribed to. The caller applies the subscription
/// filter itself, once each broadcaster has been recorded.
/// </param>
public record ScheduleQuery(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<string> LeagueSlugs,
    IReadOnlyList<string> BroadcasterSlugs);

public record LeagueKey(string SportSlug, string LeagueSlug, string? LeagueExternalId = null);

public record TeamKey(LeagueKey League, string TeamExternalId);

public record EventKey(string SportSlug, string LeagueSlug, string EventExternalId);
