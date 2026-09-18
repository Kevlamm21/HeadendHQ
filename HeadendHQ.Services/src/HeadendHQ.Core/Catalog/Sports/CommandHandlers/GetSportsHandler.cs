using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Sports.CommandHandlers;

public record GetSportsQuery : IQuery<IReadOnlyList<Sport>>;

public class GetSportsHandler(IReadModel readModel) : IQueryHandler<GetSportsQuery, IReadOnlyList<Sport>>
{
    public async ValueTask<IReadOnlyList<Sport>> Handle(GetSportsQuery query, CancellationToken ct) =>
        await readModel.All<Sport>(ct);
}
