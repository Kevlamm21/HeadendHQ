using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Teams;

namespace HeadendHQ.Core.Events;

public sealed class EventFollowFilter
{
    private readonly Dictionary<int, HashSet<string>> _externalIdsByLeague = [];
    private readonly Dictionary<int, HashSet<string>> _namesByLeague = [];

    public EventFollowFilter(IEnumerable<Team> followedTeams, string sourceKey)
    {
        foreach (var team in followedTeams)
        {
            Bucket(_namesByLeague, team.LeagueId, StringComparer.OrdinalIgnoreCase).Add(team.DisplayName);

            if (team.ExternalIdFor(sourceKey) is { Length: > 0 } externalId)
                Bucket(_externalIdsByLeague, team.LeagueId, StringComparer.Ordinal).Add(externalId);
        }
    }

    public bool IsFollowed(League league, ScheduledEventDescriptor descriptor) =>
        league.IsFollowed &&
        (!league.SupportsTeams || descriptor.Competitors.Any(c => IsFollowedTeam(league.Id, c)));

    private bool IsFollowedTeam(int leagueId, CompetitorDescriptor competitor) =>
        (competitor.TeamExternalId is { Length: > 0 } externalId
            && _externalIdsByLeague.TryGetValue(leagueId, out var ids) && ids.Contains(externalId))
        || (_namesByLeague.TryGetValue(leagueId, out var names) && names.Contains(competitor.DisplayName));

    private static HashSet<string> Bucket(
        Dictionary<int, HashSet<string>> map, int leagueId, StringComparer comparer)
    {
        if (!map.TryGetValue(leagueId, out var set))
            map[leagueId] = set = new HashSet<string>(comparer);

        return set;
    }
}
