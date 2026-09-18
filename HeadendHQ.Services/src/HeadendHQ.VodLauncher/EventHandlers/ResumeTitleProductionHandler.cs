using Hangfire;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.VodLauncher.EventHandlers;

public record ResumeTitleProductionCommand : ICommand<ResumeTitleProductionResult>;

public record ResumeTitleProductionResult(int AdbMappings, int Productions);

public class ResumeTitleProductionHandler(
    IReadModel readModel,
    IBackgroundJobClient jobClient) : ICommandHandler<ResumeTitleProductionCommand, ResumeTitleProductionResult>
{
    public async ValueTask<ResumeTitleProductionResult> Handle(ResumeTitleProductionCommand command, CancellationToken ct)
    {
        var pendingAdb = await readModel.Search<Title, Title>(new PendingAdbMappingSpec(DateTime.UtcNow), ct);
        foreach (var title in pendingAdb)
            TitleProductionJobs.EnqueueAdbMapping(jobClient, title.Id);

        var pendingProduction = await readModel.Search<Title, Title>(new TitlesNeedingProductionTodaySpec(), ct);
        foreach (var title in pendingProduction)
            TitleProductionJobs.EnqueueProduction(jobClient, title.Id);

        return new ResumeTitleProductionResult(pendingAdb.Count, pendingProduction.Count);
    }
}
