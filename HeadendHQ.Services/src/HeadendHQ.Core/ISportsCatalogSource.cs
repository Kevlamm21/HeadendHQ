using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams;

namespace HeadendHQ.Core;

public record LeagueKey(string SportSlug, string LeagueSlug);

public record TeamKey(LeagueKey League, string TeamExternalId);

public interface ISportsCatalogSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<SportRequest>> GetSportsAsync(CancellationToken ct);

    Task<IReadOnlyList<LeagueRequest>> GetLeaguesAsync(string sportSlug, CancellationToken ct);

    Task<LeagueRequest?> GetLeagueAsync(string sportSlug, string leagueSlug, CancellationToken ct);

    Task<IReadOnlyList<TeamRequest>> GetTeamsAsync(LeagueKey league, CancellationToken ct);

    Task<IReadOnlyList<LogoRequest>> GetTeamLogosAsync(TeamKey team, CancellationToken ct);
}
