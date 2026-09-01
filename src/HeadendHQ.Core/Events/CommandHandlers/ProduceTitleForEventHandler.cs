using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

/// <summary>
/// The single place where the sports domain is flattened into a title.
/// <para>
/// Every logo becomes an image id and every athlete becomes a name, a role and a headshot id, so the
/// NFO writer, artwork composer and ADB mapper never have to know what a league is. A video game or
/// a movie will map into the same shape from its own entity.
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
        var cast = await BuildCastAsync(sportingEvent, ct);

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
    /// Resolves each logo to an image id, downloading the bytes if this is the first time they have
    /// actually been needed. Doing it here means the artwork composer only ever reads by id.
    /// </summary>
    private async Task<TitleArtwork> BuildArtworkAsync(
        SportingEvent sportingEvent, League league, Team? homeTeam, Team? awayTeam,
        Broadcaster? broadcaster, CancellationToken ct)
    {
        var homeLogoId = await MaterializeAsync(homeTeam?.PreferredLogo()?.Image, ImagePurpose.TeamLogo, ct);
        var awayLogoId = await MaterializeAsync(awayTeam?.PreferredLogo()?.Image, ImagePurpose.TeamLogo, ct);

        // Variant first, so an NBA Cup game gets the Cup mark when one has been provided.
        var badgeId = await MaterializeAsync(
            league.LogoFor(sportingEvent.Variant)?.Image, ImagePurpose.LeagueLogo, ct);

        var providerId = await MaterializeAsync(
            broadcaster?.PreferredLogo()?.Image, ImagePurpose.BroadcasterLogo, ct);

        var wordmarkId = league.WordmarkFor(sportingEvent.Variant)?.Image.ImageId;

        return TitleArtwork.Create(
            homeLogoId, awayLogoId,
            homeTeam?.PrimaryColorHex, awayTeam?.PrimaryColorHex,
            badgeId, providerId, wordmarkId);
    }

    private async Task<int?> MaterializeAsync(
        Media.ImageRef? slot, ImagePurpose purpose, CancellationToken ct)
    {
        if (slot is null)
            return null;

        return slot.IsMaterialized
            ? slot.ImageId
            : await mediator.Send(new MaterializeImageCommand(slot, purpose), ct);
    }

    private async Task<List<TitleCastRequest>> BuildCastAsync(SportingEvent sportingEvent, CancellationToken ct)
    {
        var ordered = sportingEvent.Cast.OrderBy(c => c.Order).ToList();

        if (ordered.Count == 0)
            return [];

        var athletes = (await workspace.Load(
                new AthletesByIdsSpec([.. ordered.Select(c => c.AthleteId)]), ct))
            .ToDictionary(a => a.Id);

        var cast = new List<TitleCastRequest>();

        foreach (var member in ordered)
        {
            if (!athletes.TryGetValue(member.AthleteId, out var athlete))
                continue;

            var teamName = athlete.TeamId == sportingEvent.HomeTeamId
                ? sportingEvent.HomeTeamName
                : athlete.TeamId == sportingEvent.AwayTeamId
                    ? sportingEvent.AwayTeamName
                    : null;

            var role = string.Join(", ", new[] { athlete.Position, teamName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            cast.Add(new TitleCastRequest(
                athlete.DisplayName,
                role.Length > 0 ? role : null,
                athlete.Headshot.ImageId));
        }

        return cast;
    }

    private static string SeasonLabel(int? seasonType) => seasonType switch
    {
        1 => "Preseason",
        3 => "Postseason",
        _ => "Regular Season",
    };
}
