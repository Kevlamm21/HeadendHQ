using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Media.CommandHandlers;

public record GetImageQuery(int Id) : IQuery<Image?>;

public class GetImageHandler(IReadModel readModel) : IQueryHandler<GetImageQuery, Image?>
{
    public async ValueTask<Image?> Handle(GetImageQuery query, CancellationToken ct) =>
        await readModel.SingleOrDefault(new ImageByIdSpec(query.Id), ct);
}
