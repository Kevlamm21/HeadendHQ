using HeadendHQ.Core.Catalog.Leagues.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record GetLeaguesQuery(int? SportId, bool? FollowedOnly) : IQuery<IReadOnlyList<League>>;

public class GetLeaguesHandler(IReadModel readModel) : IQueryHandler<GetLeaguesQuery, IReadOnlyList<League>>
{
    public async ValueTask<IReadOnlyList<League>> Handle(GetLeaguesQuery query, CancellationToken ct)
    {
        var leagues = query.SportId is { } sportId
            ? await readModel.Search(new LeaguesBySportSpec(sportId), ct)
            : query.FollowedOnly == true
                ? await readModel.Search(new FollowedLeaguesSpec(), ct)
                : await readModel.All<League>(ct);

        return query.FollowedOnly == true
            ? [.. leagues.Where(l => l.IsFollowed)]
            : leagues;
    }
}
