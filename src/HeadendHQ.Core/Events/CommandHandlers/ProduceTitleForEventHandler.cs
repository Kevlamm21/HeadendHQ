using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

/// <summary>Maps one event into a title. Safe to call twice: an event that has one is skipped.</summary>
public record ProduceTitleForEventCommand(Guid SportingEventId) : ICommand<Guid?>;

/// <summary>
/// Creates the <see cref="Title"/> for a due event: the launch slug (which the title needs up front
/// for ADB mapping) plus <see cref="SportingEventTitleMapper"/> for the name, dates and NFO metadata.
/// Artwork is composed later from the event via <see cref="Title.SourceId"/>.
/// </summary>
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

        var launchSlug = await ResolveLaunchSlugAsync(broadcaster, ct);

        var request = SportingEventTitleMapper.ToTitleRequest(sportingEvent, league, launchSlug);

        var title = await mediator.Send(new CreateTitleCommand(request), ct);
        sportingEvent.AttachTitle(title.Id);

        logger.LogInformation("Produced title {Title} for {Away} at {Home}.",
            title.Name, sportingEvent.AwayTeamName, sportingEvent.HomeTeamName);

        return title.Id;
    }

    /// <summary>
    /// What the launcher should open. An IPTV channel tunes on an aerial; a hand-set
    /// <see cref="Broadcaster.MapsToBroadcasterId"/> redirects a local affiliate to a launchable
    /// service. One hop only.
    /// </summary>
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
