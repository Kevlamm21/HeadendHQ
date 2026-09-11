using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record ProduceTitleForEventCommand(Guid SportingEventId) : ICommand<Guid?>;

public class ProduceTitleForEventHandler(
    IWorkspace workspace,
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

        var broadcaster = sportingEvent.BroadcasterId is { } broadcasterId
            ? await workspace.LoadById<Broadcaster, int>(broadcasterId, ct)
            : null;

        // TODO(deep-links): only produce a title when the deep-link catalog says the event can be launched by intent.

        var launchSlug = await ResolveLaunchSlugAsync(broadcaster, ct);

        var request = SportingEventTitleMapper.ToTitleRequest(sportingEvent, league, launchSlug);

        var title = await mediator.Send(new CreateTitleCommand(request), ct);
        sportingEvent.AttachTitle(title.Id);

        logger.LogInformation("Produced title {Title} for {Away} at {Home}.",
            title.Name, sportingEvent.AwayTeamName, sportingEvent.HomeTeamName);

        return title.Id;
    }

    private async Task<string?> ResolveLaunchSlugAsync(Broadcaster? broadcaster, CancellationToken ct)
    {
        if (broadcaster is null)
            return null;

        if (broadcaster.IptvGuideNumber is { Length: > 0 } channel)
            return channel;

        if (broadcaster.MapsToBroadcasterId is not { } targetId)
            return broadcaster.Slug;

        var target = await workspace.LoadById<Broadcaster, int>(targetId, ct);

        logger.LogDebug("Broadcaster {From} is mapped to {To}.", broadcaster.Slug, target.Slug);

        return target.IptvGuideNumber is { Length: > 0 } targetChannel ? targetChannel : target.Slug;
    }
}
