using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record GetTitleByIdQuery(Guid Id) : ICommand<Title?>;

public class GetTitleByIdHandler(IReadModel readModel)
    : ICommandHandler<GetTitleByIdQuery, Title?>
{
    public async ValueTask<Title?> Handle(GetTitleByIdQuery query, CancellationToken ct)
    {
        var results = await readModel.Search(new TitleByIdSpec(query.Id), ct);
        return results.FirstOrDefault();
    }
}
