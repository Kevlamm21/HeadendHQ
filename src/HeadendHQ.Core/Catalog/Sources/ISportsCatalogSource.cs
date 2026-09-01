namespace HeadendHQ.Core.Catalog.Sources;

/// <summary>
/// Supplies the reference data behind sports, leagues, teams and rosters. Implemented by
/// <c>HeadendHQ.Espn</c> today; the interface exists so that stays an implementation detail.
/// </summary>
public interface ISportsCatalogSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<SportDescriptor>> GetSportsAsync(CancellationToken ct);

    Task<IReadOnlyList<LeagueDescriptor>> GetLeaguesAsync(string sportSlug, CancellationToken ct);

    /// <summary>Every team in a league, with colours and logo URLs, in as few calls as possible.</summary>
    Task<IReadOnlyList<TeamDescriptor>> GetTeamsAsync(LeagueKey league, CancellationToken ct);

    /// <summary>
    /// The logo variants a source can only supply per team, when its bulk listing cannot be trusted
    /// for them. Called once per team and cached, so it must be safe to skip entirely.
    /// </summary>
    Task<IReadOnlyList<ImageCandidate>> GetTeamLogosAsync(TeamKey team, CancellationToken ct);

    Task<IReadOnlyList<AthleteDescriptor>> GetRosterAsync(TeamKey team, CancellationToken ct);

    /// <summary>Positional ranks for a team, keyed by athlete external id. Empty when unsupported.</summary>
    Task<IReadOnlyDictionary<string, int>> GetDepthChartAsync(TeamKey team, int seasonYear, CancellationToken ct);
}
