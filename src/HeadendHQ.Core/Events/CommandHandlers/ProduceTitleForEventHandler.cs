using HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;
using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.Core.Titles;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

/// <summary>Maps one event into a title. Safe to call twice: an event that has one is skipped.</summary>
public record ProduceTitleForEventCommand(Guid SportingEventId) : ICommand<Guid?>;

/// <summary>
/// The single place where the sports domain is flattened into a title.
/// <para>
/// Every logo becomes an image id, so the NFO writer, artwork composer and ADB mapper never have to
/// know what a league is. A video game or a movie will map into the same shape from its own entity.
/// </para>
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

        var title = await ProduceAsync(sportingEvent, ct);
        sportingEvent.AttachTitle(title.Id);

        logger.LogInformation("Produced title {Title} for {Away} at {Home}.",
            title.Name, sportingEvent.AwayTeamName, sportingEvent.HomeTeamName);

        return title.Id;
    }

    private async Task<Title> ProduceAsync(SportingEvent sportingEvent, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(sportingEvent.LeagueId, ct);

        var homeTeam = sportingEvent.HomeTeamId is { } homeId
            ? await workspace.LoadById<Team, int>(homeId, ct) : null;
        var awayTeam = sportingEvent.AwayTeamId is { } awayId
            ? await workspace.LoadById<Team, int>(awayId, ct) : null;
        var broadcaster = sportingEvent.BroadcasterId is { } broadcasterId
            ? await workspace.LoadById<Broadcaster, int>(broadcasterId, ct) : null;

        var carrier = await ResolveCarrierAsync(broadcaster, ct);

        var artwork = await BuildArtworkAsync(sportingEvent, league, homeTeam, awayTeam, carrier.LogoSource, ct);
        var cast = BuildCast(sportingEvent);

        var request = new TitleRequest
        {
            // Home first, matching the poster: home takes the top half and the left half, so the
            // name reads in the same order as the picture.
            Name = $"{sportingEvent.HomeTeamName} vs {sportingEvent.AwayTeamName}",
            Type = TitleType.SportingEvent,
            LaunchSlug = carrier.LaunchSlug,
            EventUrl = sportingEvent.WatchUrl,
            StartUtc = sportingEvent.StartUtc,
            EndUtc = sportingEvent.EndUtc,
            Artwork = artwork,
            Cast = cast,
            Metadata = new TitleMetadata
            {
                Studio = league.Name,
                Genres = ["Sports", league.Name],
                Sets = [league.Name],
                Year = sportingEvent.SeasonYear ?? sportingEvent.StartUtc.Year,
                Tagline = sportingEvent.Note ?? $"{league.Name} {SeasonLabel(sportingEvent.SeasonType)}",
                Plot = $"{sportingEvent.AwayTeamName} at {sportingEvent.HomeTeamName}.",
                VenueName = sportingEvent.VenueName,
                UniqueId = $"{sportingEvent.SourceKey.ToLowerInvariant()}:{sportingEvent.ExternalId}",
            },
        };

        return await mediator.Send(new CreateTitleCommand(request), ct);
    }

    /// <summary>
    /// Which broadcaster's mark goes on the artwork, and what the launcher should be told.
    /// <para>
    /// ESPN names the local affiliate that actually carries a game — KTRK, FOX32, Packers TV Network
    /// — and those are neither launchable nor logoed. A hand-set mapping redirects them: pointing one
    /// at ESPN swaps in ESPN's identity outright, while pointing it at an HDHomeRun channel keeps its
    /// own mark (ABC on an aerial is still ABC) and only changes what gets tuned.
    /// </para>
    /// </summary>
    private async Task<(Broadcaster? LogoSource, string? LaunchSlug)> ResolveCarrierAsync(
        Broadcaster? broadcaster, CancellationToken ct)
    {
        if (broadcaster is null)
            return (null, null);

        if (broadcaster.IptvGuideNumber is { Length: > 0 } channel)
            return (broadcaster, channel);

        if (broadcaster.MapsToBroadcasterId is not { } targetId)
            return (broadcaster, broadcaster.Slug);

        // One hop only. A chain would be a configuration mistake rather than a feature, and this way
        // a mapping that loops back cannot hang title production.
        var target = await workspace.LoadById<Broadcaster, int>(targetId, ct);

        logger.LogDebug("Broadcaster {From} is mapped to {To}.", broadcaster.Slug, target.Slug);

        return (target, target.IptvGuideNumber is { Length: > 0 } targetChannel ? targetChannel : target.Slug);
    }

    /// <summary>
    /// Flattens each catalog record's chosen mark to an image id. A logo row always has bytes, so
    /// this is a read — the downloading happened when the league was followed, the team picked or the
    /// broadcaster subscribed.
    /// <para>
    /// The one exception is a broadcaster that has never had artwork: the crawl skips it for the
    /// ~1300 networks it walks, and a game airing on one is the first evidence it is worth holding.
    /// </para>
    /// </summary>
    private async Task<TitleArtwork> BuildArtworkAsync(
        SportingEvent sportingEvent, League league, Team? homeTeam, Team? awayTeam,
        Broadcaster? broadcaster, CancellationToken ct)
    {
        var homeLogoId = homeTeam?.PreferredLogo()?.ImageId;
        var awayLogoId = awayTeam?.PreferredLogo()?.ImageId;

        // Variant first, so an NBA Cup game gets the Cup mark when one has been provided.
        var badgeId = league.LogoFor(sportingEvent.Variant)?.ImageId;

        var providerId = broadcaster is null
            ? null
            : await ProviderLogoAsync(broadcaster, ct);

        var wordmarkId = league.WordmarkFor(sportingEvent.Variant)?.ImageId;

        return TitleArtwork.Create(
            homeLogoId, awayLogoId,
            homeTeam?.PrimaryColorHex, awayTeam?.PrimaryColorHex,
            badgeId, providerId, wordmarkId);
    }

    private async Task<int?> ProviderLogoAsync(Broadcaster broadcaster, CancellationToken ct)
    {
        if (broadcaster.PreferredLogo() is { } held)
            return held.ImageId;

        // A missing mark must never fail the title that wanted it; artwork falls back.
        try
        {
            await mediator.Send(new RefreshBroadcasterLogosCommand(broadcaster.Id), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to resolve artwork for broadcaster {Slug}.", broadcaster.Slug);
        }

        return broadcaster.PreferredLogo()?.ImageId;
    }

    /// <summary>
    /// The event already holds its cast as names, roles and image ids — the ranker resolved all of
    /// that when the event was scraped — so this is a projection, not a lookup.
    /// </summary>
    private static List<TitleCastRequest> BuildCast(SportingEvent sportingEvent) =>
        [.. sportingEvent.Cast
            .OrderBy(member => member.Order)
            .Select(member => new TitleCastRequest(member.Name, member.Role, member.HeadshotImageId))];

    private static string SeasonLabel(int? seasonType) => seasonType switch
    {
        1 => "Preseason",
        3 => "Postseason",
        _ => "Regular Season",
    };
}
