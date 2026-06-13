using HeadendHQ.Core.Assets.CommandHandlers;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Web.Assets;

public static class AssetEndpoints
{
    public static void MapAssetEndpoints(this WebApplication app)
    {
        MapLeagueAssetEndpoints(app);
        MapTeamAssetEndpoints(app);
        MapStreamingServiceAssetEndpoints(app);
        MapWordMarkEndpoints(app);
    }

    private static void MapLeagueAssetEndpoints(WebApplication app)
    {
        app.MapGet("/assets/leagues", async (IMediator mediator, CancellationToken ct) =>
        {
            var assets = await mediator.Send(new GetLeagueAssetsQuery(), ct);
            return Results.Ok(assets);
        })
        .WithTags("League Assets")
        .WithName("GetLeagueAssets")
        .WithSummary("List league assets")
        .WithDescription("Returns all league logo assets.");

        app.MapPost("/assets/leagues", async (CreateLeagueAssetCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var asset = await mediator.Send(command, ct);
            return Results.Created($"/assets/leagues/{asset.Id}", asset);
        })
        .WithTags("League Assets")
        .WithName("CreateLeagueAsset")
        .WithSummary("Create league asset")
        .WithDescription("Creates a new league asset variant (e.g. 'NBA Finals'). Default variants are auto-seeded on startup.");

        app.MapPut("/assets/leagues/{league}/{variant}/logo", async (League league, string variant, IFormFile logo, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var logoData = await ReadBytesAsync(logo);
                var asset = await mediator.Send(new UploadLeagueLogoCommand(league, variant, logoData), ct);
                return Results.Ok(asset);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("League Assets")
        .WithName("UploadLeagueLogo")
        .WithSummary("Upload league logo")
        .WithDescription("Uploads and normalizes the logo for a league asset.")
        .DisableAntiforgery();

        app.MapDelete("/assets/leagues/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.Send(new DeleteLeagueAssetCommand(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("League Assets")
        .WithName("DeleteLeagueAsset")
        .WithSummary("Delete league asset")
        .WithDescription("Deletes a league asset by ID.");
    }

    private static void MapTeamAssetEndpoints(WebApplication app)
    {
        app.MapGet("/assets/teams", async (IMediator mediator, CancellationToken ct) =>
        {
            var assets = await mediator.Send(new GetTeamAssetsQuery(), ct);
            return Results.Ok(assets);
        })
        .WithTags("Team Assets")
        .WithName("GetTeamAssets")
        .WithSummary("List team assets")
        .WithDescription("Returns all team assets.");

        app.MapPost("/assets/teams", async (CreateTeamAssetCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var asset = await mediator.Send(command, ct);
            return Results.Created($"/assets/teams/{asset.Id}", asset);
        })
        .WithTags("Team Assets")
        .WithName("CreateTeamAsset")
        .WithSummary("Create team asset")
        .WithDescription("Creates a team asset with metadata. Upload the logo separately via PUT /assets/teams/{league}/{teamName}/logo.");

        app.MapPut("/assets/teams/{id:int}", async (int id, UpdateTeamAssetCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var asset = await mediator.Send(command with { Id = id }, ct);
                return Results.Ok(asset);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Team Assets")
        .WithName("UpdateTeamAsset")
        .WithSummary("Update team asset")
        .WithDescription("Updates team metadata (name, league, colors). Does not affect the stored logo.");

        app.MapPut("/assets/teams/{league}/{teamName}/logo", async (League league, string teamName, IFormFile logo, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var logoData = await ReadBytesAsync(logo);
                var asset = await mediator.Send(new UploadTeamLogoCommand(teamName, league, logoData), ct);
                return Results.Ok(asset);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Team Assets")
        .WithName("UploadTeamLogo")
        .WithSummary("Upload team logo")
        .WithDescription("Uploads and normalizes the logo for a team asset.")
        .DisableAntiforgery();

        app.MapDelete("/assets/teams/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.Send(new DeleteTeamAssetCommand(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Team Assets")
        .WithName("DeleteTeamAsset")
        .WithSummary("Delete team asset")
        .WithDescription("Deletes a team asset by ID.");
    }

    private static void MapStreamingServiceAssetEndpoints(WebApplication app)
    {
        app.MapGet("/assets/streaming", async (IMediator mediator, CancellationToken ct) =>
        {
            var assets = await mediator.Send(new GetStreamingServiceAssetsQuery(), ct);
            return Results.Ok(assets);
        })
        .WithTags("Streaming Assets")
        .WithName("GetStreamingServiceAssets")
        .WithSummary("List streaming service assets")
        .WithDescription("Returns all streaming service assets. One record per service is seeded on startup.");

        app.MapPut("/assets/streaming/{service}/logo", async (StreamingService service, IFormFile logo, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var logoData = await ReadBytesAsync(logo);
                var asset = await mediator.Send(new UploadStreamingLogoCommand(service, logoData), ct);
                return Results.Ok(asset);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Streaming Assets")
        .WithName("UploadStreamingLogo")
        .WithSummary("Upload streaming service logo")
        .WithDescription("Uploads and normalizes the logo for a streaming service asset.")
        .DisableAntiforgery();
    }

    private static void MapWordMarkEndpoints(WebApplication app)
    {
        app.MapGet("/assets/wordmarks", async (IMediator mediator, CancellationToken ct) =>
        {
            var wordMarks = await mediator.Send(new GetWordMarksQuery(), ct);
            return Results.Ok(wordMarks);
        })
        .WithTags("Word Marks")
        .WithName("GetWordMarks")
        .WithSummary("List word marks")
        .WithDescription("Returns all word mark assets. One 'Original' variant per league is seeded on startup.");

        app.MapPost("/assets/wordmarks", async (CreateWordMarkCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var wordMark = await mediator.Send(command, ct);
            return Results.Created($"/assets/wordmarks/{wordMark.Id}", wordMark);
        })
        .WithTags("Word Marks")
        .WithName("CreateWordMark")
        .WithSummary("Create word mark")
        .WithDescription("Creates a new word mark variant for a league.");

        app.MapPut("/assets/wordmarks/{league}/{variant}/logo", async (League league, string variant, IFormFile logo, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var logoData = await ReadBytesAsync(logo);
                var wordMark = await mediator.Send(new UploadWordMarkLogoCommand(league, variant, logoData), ct);
                return Results.Ok(wordMark);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Word Marks")
        .WithName("UploadWordMarkLogo")
        .WithSummary("Upload word mark logo")
        .WithDescription("Uploads and normalizes the logo for a word mark asset.")
        .DisableAntiforgery();

        app.MapDelete("/assets/wordmarks/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.Send(new DeleteWordMarkCommand(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Word Marks")
        .WithName("DeleteWordMark")
        .WithSummary("Delete word mark")
        .WithDescription("Deletes a word mark asset by ID.");
    }

    private static async Task<byte[]> ReadBytesAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}
