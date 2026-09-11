namespace HeadendHQ.Core.Catalog.Sources;

public interface ISportsCatalogSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<SportDescriptor>> GetSportsAsync(CancellationToken ct);

    Task<IReadOnlyList<LeagueDescriptor>> GetLeaguesAsync(string sportSlug, CancellationToken ct);

    Task<IReadOnlyList<TeamDescriptor>> GetTeamsAsync(LeagueKey league, CancellationToken ct);

    Task<IReadOnlyList<ImageCandidate>> GetTeamLogosAsync(TeamKey team, CancellationToken ct);

    Task<IReadOnlyList<AthleteDescriptor>> GetRosterAsync(TeamKey team, CancellationToken ct);

    Task<IReadOnlyDictionary<string, int>> GetDepthChartAsync(TeamKey team, int seasonYear, CancellationToken ct);
}
