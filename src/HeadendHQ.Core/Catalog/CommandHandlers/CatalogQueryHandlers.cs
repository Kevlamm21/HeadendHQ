using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Catalog.CommandHandlers;

public record GetSportsQuery : IQuery<IReadOnlyList<Sport>>;

public class GetSportsHandler(IReadModel readModel) : IQueryHandler<GetSportsQuery, IReadOnlyList<Sport>>
{
    public async ValueTask<IReadOnlyList<Sport>> Handle(GetSportsQuery query, CancellationToken ct) =>
        await readModel.All<Sport>(ct);
}

public record GetLeaguesQuery(int? SportId, bool? FollowedOnly) : IQuery<IReadOnlyList<League>>;

public class GetLeaguesHandler(IReadModel readModel) : IQueryHandler<GetLeaguesQuery, IReadOnlyList<League>>
{
    public async ValueTask<IReadOnlyList<League>> Handle(GetLeaguesQuery query, CancellationToken ct)
    {
        var leagues = query.SportId is { } sportId
            ? await readModel.Search(new LeaguesBySportSpec(sportId), ct)
            : query.FollowedOnly == true
                ? await readModel.Search(new FollowedLeaguesSpec(), ct)
                : await readModel.All<League>(ct);

        return query.FollowedOnly == true
            ? [.. leagues.Where(l => l.IsFollowed)]
            : leagues;
    }
}

public record GetTeamsQuery(int LeagueId) : IQuery<IReadOnlyList<Team>>;

public class GetTeamsHandler(IReadModel readModel) : IQueryHandler<GetTeamsQuery, IReadOnlyList<Team>>
{
    public async ValueTask<IReadOnlyList<Team>> Handle(GetTeamsQuery query, CancellationToken ct) =>
        await readModel.Search(new TeamsByLeagueSpec(query.LeagueId), ct);
}

/// <summary>Debugging aid: removes a league's teams so they can be re-pulled with fresh logos.</summary>
public record DeleteAllLeagueTeamsCommand(int LeagueId) : ICommand<int>;

public class DeleteAllLeagueTeamsHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllLeagueTeamsCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllLeagueTeamsCommand command, CancellationToken ct)
    {
        var teams = await workspace.Load(new TeamsByLeagueSpec(command.LeagueId), ct);

        foreach (var team in teams)
            workspace.Remove(team);

        return teams.Count;
    }
}

/// <summary>
/// Debugging aid: clears a league's team logo materializations and removes image blobs that no
/// longer have any reference elsewhere.
/// </summary>
public record DeleteAllLeagueTeamImagesCommand(int LeagueId) : ICommand<int>;

public class DeleteAllLeagueTeamImagesHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllLeagueTeamImagesCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllLeagueTeamImagesCommand command, CancellationToken ct)
    {
        var teams = await workspace.Load(new TeamsByLeagueSpec(command.LeagueId), ct);
        var imageIds = teams
            .SelectMany(team => team.Logos)
            .Select(logo => logo.Image.ImageId)
            .OfType<int>()
            .ToHashSet();

        foreach (var team in teams)
            foreach (var logo in team.Logos)
                logo.Image.ClearMaterialization();

        var referencedImageIds = new HashSet<int>();

        foreach (var team in await workspace.LoadAll<Team>(ct))
            AddTeamImageIds(team, referencedImageIds);

        foreach (var league in await workspace.LoadAll<League>(ct))
        {
            foreach (var logo in league.Logos)
                AddImageId(logo.Image.ImageId, referencedImageIds);

            foreach (var wordmark in league.Wordmarks)
                AddImageId(wordmark.Image.ImageId, referencedImageIds);
        }

        foreach (var broadcaster in await workspace.LoadAll<Broadcaster>(ct))
            foreach (var logo in broadcaster.Logos)
                AddImageId(logo.Image.ImageId, referencedImageIds);

        foreach (var athlete in await workspace.LoadAll<Athlete>(ct))
            AddImageId(athlete.Headshot.ImageId, referencedImageIds);

        foreach (var title in await workspace.LoadAll<Title>(ct))
        {
            AddImageId(title.Artwork.PrimaryLogoImageId, referencedImageIds);
            AddImageId(title.Artwork.SecondaryLogoImageId, referencedImageIds);
            AddImageId(title.Artwork.BadgeImageId, referencedImageIds);
            AddImageId(title.Artwork.ProviderLogoImageId, referencedImageIds);
            AddImageId(title.Artwork.WordmarkImageId, referencedImageIds);

            foreach (var castMember in title.Cast)
                AddImageId(castMember.HeadshotImageId, referencedImageIds);
        }

        var deleted = 0;
        foreach (var image in await workspace.LoadAll<Image>(ct))
        {
            if (imageIds.Contains(image.Id) && !referencedImageIds.Contains(image.Id))
            {
                workspace.Remove(image);
                deleted++;
            }
        }

        return deleted;
    }

    private static void AddTeamImageIds(Team team, HashSet<int> imageIds)
    {
        foreach (var logo in team.Logos)
            AddImageId(logo.Image.ImageId, imageIds);
    }

    private static void AddImageId(int? imageId, HashSet<int> imageIds)
    {
        if (imageId is { } id)
            imageIds.Add(id);
    }
}

/// <summary>National networks (no call-sign token), local lineup-matched affiliates, and everything else.</summary>
public enum BroadcasterAffiliateFilter
{
    National,
    Local,
    Other
}

public record GetBroadcastersQuery(bool? SubscribedOnly, BroadcasterAffiliateFilter? Affiliate = null)
    : IQuery<IReadOnlyList<Broadcaster>>;

public class GetBroadcastersHandler(IReadModel readModel)
    : IQueryHandler<GetBroadcastersQuery, IReadOnlyList<Broadcaster>>
{
    public async ValueTask<IReadOnlyList<Broadcaster>> Handle(GetBroadcastersQuery query, CancellationToken ct)
    {
        var broadcasters = query.SubscribedOnly == true
            ? await readModel.Search(new SubscribedBroadcastersSpec(), ct)
            : await readModel.All<Broadcaster>(ct);

        return query.Affiliate switch
        {
            BroadcasterAffiliateFilter.National => [.. broadcasters.Where(b => !b.IsAffiliate)],
            BroadcasterAffiliateFilter.Local => [.. broadcasters.Where(b => b.IsAffiliate && b.IptvGuideNumber is { Length: > 0 })],
            BroadcasterAffiliateFilter.Other => [.. broadcasters.Where(b => b.IsAffiliate && b.IptvGuideNumber is null or "")],
            _ => broadcasters,
        };
    }
}

public record GetCatalogSyncStateQuery : IQuery<CatalogSyncState?>;

public class GetCatalogSyncStateHandler(IReadModel readModel)
    : IQueryHandler<GetCatalogSyncStateQuery, CatalogSyncState?>
{
    public async ValueTask<CatalogSyncState?> Handle(GetCatalogSyncStateQuery query, CancellationToken ct) =>
        await readModel.SingleOrDefault(new CatalogSyncStateSpec(), ct);
}

/// <summary>
/// Following a league is the moment its teams become worth fetching, so the follow does that work
/// rather than leaving the user with an empty team picker.
/// </summary>
public record FollowLeagueCommand(int LeagueId, bool Followed) : ICommand<League>;

public class FollowLeagueHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<FollowLeagueCommand, League>
{
    public async ValueTask<League> Handle(FollowLeagueCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);
        league.Follow(command.Followed);

        if (command.Followed && league.SupportsTeams && league.TeamsRefreshedAtUtc is null)
            await mediator.Send(new RefreshLeagueTeamsCommand(league.Id), ct);

        if (command.Followed)
            foreach (var logo in league.Logos)
                await mediator.Send(new MaterializeImageCommand(logo.Image, ImagePurpose.LeagueLogo), ct);

        return league;
    }
}

public record UpdateTeamCommand(
    int TeamId, bool? Followed, string? PreferredLogoRel,
    string? PrimaryColorHex, string? AlternateColorHex) : ICommand<Team>;

public class UpdateTeamHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UpdateTeamCommand, Team>
{
    public async ValueTask<Team> Handle(UpdateTeamCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        if (command.Followed is { } followed) team.Follow(followed);
        if (command.PreferredLogoRel is { Length: > 0 } rel) team.PreferLogo(rel);
        team.OverrideColors(command.PrimaryColorHex, command.AlternateColorHex);

        // A followed team's artwork is needed soon; fetch it now rather than mid-scrape.
        if (team.IsFollowed && team.PreferredLogo() is { } logo && !logo.Image.IsMaterialized)
            await mediator.Send(new MaterializeImageCommand(logo.Image, ImagePurpose.TeamLogo), ct);

        return team;
    }
}

/// <summary>
/// <paramref name="SetMapping"/> is what distinguishes "leave the mapping alone" from "clear it":
/// both targets are nullable, so their absence alone cannot say which was meant.
/// </summary>
public record UpdateBroadcasterCommand(
    int BroadcasterId,
    bool? Subscribed,
    bool SetMapping = false,
    int? MapsToBroadcasterId = null,
    string? IptvGuideNumber = null) : ICommand<Broadcaster>;

public class UpdateBroadcasterHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UpdateBroadcasterCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(UpdateBroadcasterCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        if (command.Subscribed is { } subscribed) broadcaster.Subscribe(subscribed);

        if (command.SetMapping)
        {
            // Loaded rather than trusted: pointing at a broadcaster that does not exist would only
            // surface much later, as a title produced with no launch target at all.
            if (command.MapsToBroadcasterId is { } targetId)
                _ = await workspace.LoadById<Broadcaster, int>(targetId, ct);

            broadcaster.SetMapping(command.MapsToBroadcasterId, command.IptvGuideNumber);
        }

        if (broadcaster.IsSubscribed && broadcaster.PreferredLogo() is { } logo)
            await mediator.Send(new MaterializeImageCommand(logo.Image, ImagePurpose.BroadcasterLogo), ct);

        return broadcaster;
    }
}
