using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record GetTitleByIdQuery(Guid Id) : ICommand<Title>;

public class GetTitleByIdHandler(IReadModel readModel)
    : ICommandHandler<GetTitleByIdQuery, Title>
{
    public async ValueTask<Title> Handle(GetTitleByIdQuery query, CancellationToken ct)
    {
        var title = await readModel.SingleOrDefault(new EntityByIdSpecification<Title, Guid>(query.Id), ct);
        return title ?? throw new NotFoundException<Title>(query.Id.ToString());
    }
}
