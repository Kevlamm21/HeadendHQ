using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record GetTeamsQuery(int LeagueId) : IQuery<IReadOnlyList<Team>>;

public class GetTeamsHandler(IReadModel readModel) : IQueryHandler<GetTeamsQuery, IReadOnlyList<Team>>
{
    public async ValueTask<IReadOnlyList<Team>> Handle(GetTeamsQuery query, CancellationToken ct) =>
        await readModel.Search(new TeamsByLeagueSpec(query.LeagueId), ct);
}
