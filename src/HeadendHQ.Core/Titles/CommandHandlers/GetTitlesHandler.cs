using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record GetTitlesQuery(DateTime? From, DateTime? To, TitleType? Type)
    : ICommand<List<Title>>;

public class GetTitlesHandler(IReadModel readModel)
    : ICommandHandler<GetTitlesQuery, List<Title>>
{
    public async ValueTask<List<Title>> Handle(GetTitlesQuery query, CancellationToken ct)
    {
        var results = await readModel.Search(new TitlesByFilterSpec(query.From, query.To, query.Type), ct);
        return [.. results];
    }
}
