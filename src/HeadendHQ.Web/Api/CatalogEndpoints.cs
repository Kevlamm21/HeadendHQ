using Hangfire;
using HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;
using HeadendHQ.Core.Catalog.Leagues.CommandHandlers;
using HeadendHQ.Core.Catalog.Sports.CommandHandlers;
using HeadendHQ.Core.Catalog.Teams.CommandHandlers;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Web.Jobs;
using Mediator;

namespace HeadendHQ.Web.Api;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var catalog = app.MapGroup("/catalog").WithTags("Sports Catalog");

        catalog.MapGet("/sports", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetSportsQuery(), ct)))
            .WithName("GetSports")
            .WithSummary("List sports");

        catalog.MapGet("/leagues", async (int? sportId, bool? followed, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetLeaguesQuery(sportId, followed), ct)))
            .WithName("GetLeagues")
            .WithSummary("List leagues")
            .WithDescription("Optionally filtered by sport, or to those the user follows.");

        catalog.MapGet("/leagues/{id:int}/teams", async (int id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetTeamsQuery(id), ct)))
            .WithName("GetLeagueTeams")
            .WithSummary("List a Teams per League");

        catalog.MapGet("/broadcasters", async (bool? subscribed, string? affiliate, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(
                new GetBroadcastersQuery(subscribed, ParseAffiliateFilter(affiliate)), ct)))
            .WithName("GetBroadcasters")
            .WithSummary("List broadcasters")
            .WithDescription("Optionally filtered to subscribed only, or by affiliate=national|local|other.");

        catalog.MapPatch("/leagues/{id:int}", async (int id, FollowRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new FollowLeagueCommand(id, body.IsFollowed), ct)))
            .WithName("FollowLeague")
            .WithSummary("Follow or unfollow a league")
            .WithDescription(
                "Following a league pulls its teams and downloads its own mark. The league catalog records no artwork — "
                + "356 leagues would mean 356 downloads for the handful anyone follows — so this is the first moment it is fetched.");

        catalog.MapPatch("/teams/{id:int}", async (int id, UpdateTeamRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateTeamCommand(
                id, body.IsFollowed, body.PreferredLogoRel, body.PrimaryColorHex, body.AlternateColorHex), ct)))
            .WithName("UpdateTeam")
            .WithSummary("Update a team")
            .WithDescription("Follow the team, pick which logo variant artwork uses, or override its colours.");
        
        catalog.MapPatch("/broadcasters/{id:int}", async (int id, UpdateBroadcasterRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateBroadcasterCommand(
                id, body.IsSubscribed,
                body.SetMapping, body.MapsToBroadcasterId, body.IptvGuideNumber), ct)))
            .WithName("UpdateBroadcaster")
            .WithSummary("Subscribe to a broadcaster, or point it at another")
            .WithDescription(
                "Send setMapping=true with mapsToBroadcasterId to make a network borrow another's identity — ABC pointed at ESPN puts the ESPN mark on the poster and launches the ESPN app — " +
                "or with iptvGuideNumber to tune an IPTV channel instead, which keeps the network's own mark. Send setMapping=true with neither to clear the mapping.");

        catalog.MapPost("/broadcasters/discover", (int? max) =>
        {
            var jobId = BackgroundJob.Enqueue<BroadcasterDiscoveryJob>(
                job => job.RunAsync(max, CancellationToken.None));
            return Results.Accepted($"/hangfire/jobs/details/{jobId}", new { jobId, max });
        })
            .WithName("DiscoverBroadcasters")
            .WithSummary("Crawl ESPN's media index for the full broadcaster catalogue")
            .WithDescription("Enqueues the discovery crawl (its own request budget). A completed crawl only re-checks the index; ?max= caps records resolved this run.");

        catalog.MapPost("/sports/{slug}/sync", async (string slug, IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { sport = slug, leagues = await mediator.Send(new SyncSportLeaguesCommand(slug), ct) }))
            .WithName("SyncSportLeagues")
            .WithSummary("Pull one sport's leagues")
            .WithDescription("For sports outside the discovery allowlist. Idempotent, so re-running it only refreshes.");
        
        catalog.MapPost("/leagues/{id:int}/refresh", async (int id, bool? withLogos, IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { teams = await mediator.Send(new RefreshLeagueTeamsCommand(id, withLogos ?? false), ct) }))
            .WithName("RefreshLeagueTeams")
            .WithSummary("Re-pull a league's teams")
            .WithDescription(
                "One upstream request returns every team with colours and logo variants. Teams with no mark yet get one either way; "
                + "withLogos=true additionally re-checks the marks already held, which an ordinary refresh skips.");
        
        catalog.MapPost("/broadcasters/resolve", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { resolved = await mediator.Send(new RefreshBroadcasterDetailsCommand(), ct) }))
            .WithName("ResolveBroadcasters")
            .WithSummary("Resolve outstanding broadcaster records")
            .WithDescription("Reads the source record — and therefore the logo — for every broadcaster that has never had one.");

        catalog.MapPost("/sync", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new SyncSportsAndLeaguesCommand(), ct)))
            .WithName("RunCatalogSync")
            .WithSummary("Force a full catalog re-sync")
            .WithDescription("Walks every sport and league again. Existing rows are updated in place, not duplicated.");

         catalog.MapDelete("/leagues/{id:int}/headshots", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var deleted = await mediator.Send(new ClearImagesByPurposeCommand(ImagePurpose.Headshot, id), ct);
            return Results.Ok(new { imagesDeleted = deleted });
        })
            .WithName("ClearLeagueHeadshots")
            .WithSummary("Throw away a league's player headshots")
            .WithDescription("Run at the start of a season, after the league's media day. Ensures future Events use the new headshots.");

        catalog.MapPut("/leagues/{id:int}/wordmarks/{variant}", async (
            int id, string variant, IFormFile logo, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UploadLeagueWordmarkCommand(id, variant, await ReadBytesAsync(logo, ct)), ct)))
            .WithName("UploadLeagueWordmark")
            .WithSummary("Upload a league wordmark")
            .WithDescription("Wordmarks are the Jellyfin clearlogo. ESPN has no equivalent asset, so these are upload-only. Variants let an NBA Cup game use a different mark from a regular-season game.")
            .DisableAntiforgery();

        catalog.MapPut("/leagues/{id:int}/logos/{variant}", async (
            int id, string variant, IFormFile logo, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UploadLeagueLogoOverrideCommand(id, variant, await ReadBytesAsync(logo, ct)), ct)))
            .WithName("UploadLeagueLogoOverride")
            .WithSummary("Replace a league logo")
            .WithDescription("A hand-uploaded image is never overwritten by a later refresh, and a refresh will not spend a request on one.")
            .DisableAntiforgery();

        catalog.MapPut("/teams/{id:int}/logos/{rel}", async (
            int id, string rel, IFormFile logo, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UploadTeamLogoOverrideCommand(id, rel, await ReadBytesAsync(logo, ct)), ct)))
            .WithName("UploadTeamLogoOverride")
            .WithSummary("Replace a team logo variant")
            .DisableAntiforgery();

        catalog.MapPut("/broadcasters/{id:int}/logo", async (
            int id, IFormFile logo, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UploadBroadcasterLogoCommand(id, await ReadBytesAsync(logo, ct)), ct)))
            .WithName("UploadBroadcasterLogo")
            .WithSummary("Replace a broadcaster logo")
            .DisableAntiforgery();

        catalog.MapDelete("/leagues/{id:int}/teams", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var deleted = await mediator.Send(new DeleteAllLeagueTeamsCommand(id), ct);
            return Results.Ok(new { deleted });
        })
            .WithName("DeleteAllLeagueTeams")
            .WithSummary("Delete all teams in a league")
            .WithDescription("Debugging aid. Removes the league's teams so they can be re-pulled with fresh logos.");

        catalog.MapDelete("/leagues/{id:int}/team-images", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var deleted = await mediator.Send(new DeleteAllLeagueTeamImagesCommand(id), ct);
            return Results.Ok(new { imagesDeleted = deleted });
        })
            .WithName("DeleteAllLeagueTeamImages")
            .WithSummary("Delete all stored team images in a league")
            .WithDescription("Debugging aid. Clears the league's team logo materializations and removes only image blobs that are no longer referenced elsewhere.");

        catalog.MapDelete("/headshots", async (IMediator mediator, CancellationToken ct) =>
        {
            var deleted = await mediator.Send(new ClearImagesByPurposeCommand(ImagePurpose.Headshot), ct);
            return Results.Ok(new { imagesDeleted = deleted });
        })
            .WithName("ClearAllHeadshots")
            .WithSummary("Throw away every stored player headshot")
            .WithDescription("Remove all Headshots & allow for new ones to be fetched. This is a debugging aid; normally only league-level wipes are needed.");

    }

    private static async Task<byte[]> ReadBytesAsync(IFormFile file, CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        return stream.ToArray();
    }

    public record FollowRequest(bool IsFollowed);

    public record UpdateTeamRequest(
        bool? IsFollowed, string? PreferredLogoRel, string? PrimaryColorHex, string? AlternateColorHex);

    public record UpdateBroadcasterRequest(
        bool? IsSubscribed,
        bool SetMapping = false,
        int? MapsToBroadcasterId = null,
        string? IptvGuideNumber = null);

    private static BroadcasterAffiliateFilter? ParseAffiliateFilter(string? value) => value?.ToLowerInvariant() switch
    {
        "national" => BroadcasterAffiliateFilter.National,
        "local" => BroadcasterAffiliateFilter.Local,
        "other" => BroadcasterAffiliateFilter.Other,
        _ => null,
    };
}
