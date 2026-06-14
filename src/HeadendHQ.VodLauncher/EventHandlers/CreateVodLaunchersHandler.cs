using Hangfire;
using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.VodLauncher.EventHandlers;

public record CreateVodLaunchersCommand : ICommand<int>;

public class CreateVodLaunchersHandler(
    IReadModel readModel,
    IBackgroundJobClient jobClient) : ICommandHandler<CreateVodLaunchersCommand, int>
{
    public async ValueTask<int> Handle(CreateVodLaunchersCommand command, CancellationToken ct)
    {
        var titles = await readModel.Search<Title, Title>(new TitlesNeedingProductionTodaySpec(), ct);

        foreach (var title in titles)
            jobClient.Enqueue<ICreationService>(s => s.CreateForTitleAsync(title.Id, CancellationToken.None));

        return titles.Count;
    }
}
