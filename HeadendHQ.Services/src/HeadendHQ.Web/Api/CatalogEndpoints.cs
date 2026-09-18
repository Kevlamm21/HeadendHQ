using Hangfire;
using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;
using HeadendHQ.Core.Catalog.CommandHandlers;
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

        catalog.MapGet("/broadcasters", async (bool? subscribed, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetBroadcastersQuery(subscribed), ct)))
            .WithName("GetBroadcasters")
            .WithSummary("List broadcasters")
            .WithDescription("Optionally filtered to broadcasters with at least one streaming assignment.");

        catalog.MapPatch("/leagues/{id:int}", async (int id, UpdateLeagueRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateLeagueCommand(id, body.IsFollowed, body.SelectedLogoId), ct)))
            .WithName("UpdateLeague")
            .WithSummary("Follow or unfollow a league, or pick its logo")
            .WithDescription(
                "Following a league pulls its team list and enqueues a background job that downloads three logos per team "
                + "(primary on primary, primary on secondary, secondary on primary). selectedLogoId picks which of the league's logos artwork uses.");

        catalog.MapPatch("/teams/{id:int}", async (int id, UpdateTeamRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateTeamCommand(
                id, body.IsFollowed, body.SelectedLogoId, body.PrimaryColorHex, body.AlternateColorHex), ct)))
            .WithName("UpdateTeam")
            .WithSummary("Update a team")
            .WithDescription("Follow the team, pick which of its logos artwork uses, or override its colours.");

        catalog.MapPut("/broadcasters/{id:int}/streaming", async (
            int id, List<StreamingAssignmentRequest> body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new SetStreamingAssignmentsCommand(id, body), ct)))
            .WithName("SetBroadcasterStreaming")
            .WithSummary("Set where a broadcaster's games can be watched")
            .WithDescription(
                "Replaces the broadcaster's assignments. A game's assignments are tried by priority (lower first): an IPTV service is used " +
                "only if the guide shows the game on that channel, a deep-link service is used directly, and an entry with no " +
                "streamingServiceId blocks the game from being imported. An empty list unsubscribes the broadcaster. " +
                "For the poster, useBroadcasterLogo passes this broadcaster's ESPN logo through; otherwise the service's " +
                "logoVariant logo is used, falling back to its Default logo.");

        catalog.MapPost("/broadcasters/discover", (int? max, bool? refresh) =>
        {
            var jobId = BackgroundJob.Enqueue<BroadcasterDiscoveryJob>(
                job => job.RunAsync(max, refresh == true, CancellationToken.None));
            return Results.Accepted($"/hangfire/jobs/details/{jobId}", new { jobId, max, refresh });
        })
            .WithName("DiscoverBroadcasters")
            .WithSummary("Crawl ESPN's media index for the full broadcaster catalogue")
            .WithDescription(
                "Enqueues the discovery crawl (its own request budget). Without refresh, a completed crawl only resolves new records. " +
                "?refresh=true re-reads every broadcaster and re-downloads its logos. ?max= caps records read this run.");

        catalog.MapPost("/sports/{slug}/sync", async (string slug, IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { sport = slug, leagues = await mediator.Send(new SyncSportLeaguesCommand(slug), ct) }))
            .WithName("SyncSportLeagues")
            .WithSummary("Pull one sport's leagues")
            .WithDescription("For sports outside the discovery allowlist. Idempotent, so re-running it only refreshes.");
        
        catalog.MapPost("/leagues/{id:int}/refresh", async (int id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { teams = await mediator.Send(new RefreshLeagueTeamsCommand(id), ct) }))
            .WithName("RefreshLeagueTeams")
            .WithSummary("Re-pull a league's teams")
            .WithDescription("One upstream request returns every team with its colours. Logos are untouched; use /leagues/{id}/teams/logos/refresh for those.");

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

        catalog.MapPost("/leagues/{id:int}/logos", async (
            int id, string? variant, IFormFile logo, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UploadLeagueLogoCommand(
                id, string.IsNullOrWhiteSpace(variant) ? LogoVariants.Default : variant, await ReadBytesAsync(logo, ct)), ct)))
            .WithName("UploadLeagueLogo")
            .WithSummary("Upload a league logo")
            .WithDescription("Adds the image to the league's logos without selecting it; select it with PATCH /catalog/leagues/{id}. ?variant= (Cup, Playoffs, …) uploads an edition badge instead. Refresh never replaces an upload.")
            .DisableAntiforgery();

        catalog.MapPost("/teams/{id:int}/logos", async (
            int id, IFormFile logo, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UploadTeamLogoCommand(id, await ReadBytesAsync(logo, ct)), ct)))
            .WithName("UploadTeamLogo")
            .WithSummary("Upload a team logo")
            .WithDescription("Adds the image to the team's logos without selecting it; select it with PATCH /catalog/teams/{id}.")
            .DisableAntiforgery();

        MapDeleteLogo(catalog, "teams", CatalogLogoOwner.Team);
        MapDeleteLogo(catalog, "leagues", CatalogLogoOwner.League);

        catalog.MapPost("/teams/{id:int}/logos/refresh", async (int id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new RefreshTeamLogosCommand(id, RefreshExisting: true), ct)))
            .WithName("RefreshTeamLogos")
            .WithSummary("Re-download a team's logos")
            .WithDescription("Replaces the stored ESPN logos with the current ones. Uploaded logos are kept.");

        catalog.MapPost("/leagues/{id:int}/logos/refresh", async (int id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new RefreshLeagueLogosCommand(id, RefreshExisting: true), ct)))
            .WithName("RefreshLeagueLogos")
            .WithSummary("Re-download a league's own logo")
            .WithDescription("Replaces the stored ESPN logo with the current one. Uploaded logos are kept.");

        catalog.MapPost("/leagues/{id:int}/teams/logos/refresh", (int id) =>
        {
            var jobId = BackgroundJob.Enqueue<TeamLogoJob>(job => job.RunAsync(id, true, CancellationToken.None));
            return Results.Accepted($"/hangfire/jobs/details/{jobId}", new { jobId });
        })
            .WithName("RefreshLeagueTeamLogos")
            .WithSummary("Re-download every team logo in a league")
            .WithDescription("Enqueues a background job; one upstream request for the team list, then the image downloads. Uploaded logos are kept.");

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
            .WithDescription("Debugging aid. Deletes the league's team logos and removes only image blobs that are no longer referenced elsewhere.");

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

    private static void MapDeleteLogo(RouteGroupBuilder catalog, string segment, CatalogLogoOwner owner) =>
        catalog.MapDelete($"/{segment}/{{id:int}}/logos/{{logoId:int}}", async (
            int id, int logoId, IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { imagesDeleted = await mediator.Send(new DeleteCatalogLogoCommand(owner, id, logoId), ct) }))
            .WithName($"Delete{owner}Logo")
            .WithSummary($"Delete an uploaded {owner.ToString().ToLowerInvariant()} logo")
            .WithDescription("Only uploaded logos can be deleted; ESPN logos are replaced by refresh. If it was selected, the default is selected instead.");

    public record UpdateLeagueRequest(bool? IsFollowed, int? SelectedLogoId = null);

    public record UpdateTeamRequest(
        bool? IsFollowed, int? SelectedLogoId, string? PrimaryColorHex, string? AlternateColorHex);
}
