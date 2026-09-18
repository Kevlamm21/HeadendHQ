using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record CollectEventDetailCommand(Guid SportingEventId) : ICommand<Unit>;

public class CollectEventDetailHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<CollectEventDetailHandler> logger)
    : ICommandHandler<CollectEventDetailCommand, Unit>
{
    public async ValueTask<Unit> Handle(CollectEventDetailCommand command, CancellationToken ct)
    {
        var sportingEvent = await workspace.LoadSingleOrDefault(
            new EntityByIdSpecification<SportingEvent, Guid>(command.SportingEventId), ct);

        if (sportingEvent is null)
        {
            logger.LogWarning("Sporting event {Id} no longer exists; skipping detail.", command.SportingEventId);
            return Unit.Value;
        }

        var detail = await mediator.Send(FetchEventDetailQuery.For(sportingEvent), ct);

        sportingEvent.ApplyDetail(detail);
        await unitOfWork.SaveChanges(ct);

        logger.LogInformation(
            "Collected detail for {Away} at {Home}: {Cast} billed, variant {Variant}.",
            sportingEvent.AwayTeamName, sportingEvent.HomeTeamName, detail.Cast.Count, sportingEvent.Variant);

        if (LocalDay.IsToday(sportingEvent.StartUtc))
            await mediator.Send(new ProduceTitleForEventCommand(sportingEvent.Id), ct);

        return Unit.Value;
    }
}
