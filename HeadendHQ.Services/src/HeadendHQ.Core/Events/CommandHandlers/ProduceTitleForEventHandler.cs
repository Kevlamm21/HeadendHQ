using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Streaming;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record ProduceTitleForEventCommand(Guid SportingEventId) : ICommand<Guid?>;

public class ProduceTitleForEventHandler(
    IWorkspace workspace,
    IReadModel readModel,
    IMediator mediator,
    ILogger<ProduceTitleForEventHandler> logger)
    : ICommandHandler<ProduceTitleForEventCommand, Guid?>
{
    public async ValueTask<Guid?> Handle(ProduceTitleForEventCommand command, CancellationToken ct)
    {
        var sportingEvent = await workspace.LoadById<SportingEvent, Guid>(command.SportingEventId, ct);

        if (!sportingEvent.NeedsTitle)
            return sportingEvent.TitleId;

        var league = await workspace.LoadById<League, int>(sportingEvent.LeagueId, ct);

        var broadcasts = sportingEvent.BroadcasterId is { } broadcasterId
            ? [(await workspace.LoadById<Broadcaster, int>(broadcasterId, ct), 0)]
            : Array.Empty<(Broadcaster, int)>();

        // TODO(deep-links): only produce a title when the deep-link catalog says the event can be launched by intent.

        var choice = await StreamingResolver.ResolveAsync(
            readModel, sportingEvent.StartUtc, sportingEvent.HomeTeamName, sportingEvent.AwayTeamName, broadcasts, ct);

        if (choice.Outcome is not StreamingOutcome.Stream)
        {
            logger.LogInformation("No title for {Away} at {Home}: {Outcome} via {Broadcaster}.",
                sportingEvent.AwayTeamName, sportingEvent.HomeTeamName, choice.Outcome, choice.Broadcaster?.Slug);
            return null;
        }

        sportingEvent.AssignStreaming(choice.Broadcaster!.Id, choice.Service!.Id);

        var request = SportingEventTitleMapper.ToTitleRequest(sportingEvent, league, choice.Service.LaunchSlug);

        var title = await mediator.Send(new CreateTitleCommand(request), ct);
        sportingEvent.AttachTitle(title.Id);

        logger.LogInformation("Produced title {Title} for {Away} at {Home} on {Service}.",
            title.Name, sportingEvent.AwayTeamName, sportingEvent.HomeTeamName, choice.Service.Key);

        return title.Id;
    }
}
