using HeadendHQ.Core;
using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Events;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using SixLabors.ImageSharp;
using ImagePurpose = HeadendHQ.Core.Media.ImagePurpose;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HeadendHQ.SixLabors;

public class ImageCreationService(
    IWorkspace workspace, IReadModel readModel, IMediator mediator)
    : IImageCreationService
{
    public Task<int> CreatePosterAsync(Guid sourceId, CancellationToken ct = default) =>
        ComposeAsync(sourceId, isVertical: true, ImagePurpose.Poster, ct);

    public Task<int> CreateThumbAsync(Guid sourceId, CancellationToken ct = default) =>
        ComposeAsync(sourceId, isVertical: false, ImagePurpose.Thumbnail, ct);

    public Task<int> CreateBackdropAsync(Guid sourceId, CancellationToken ct = default) =>
        ComposeAsync(sourceId, isVertical: false, ImagePurpose.Background, ct);

    private async Task<int> ComposeAsync(Guid sourceId, bool isVertical, ImagePurpose purpose, CancellationToken ct)
    {
        var ingredients = await ResolveIngredientsAsync(sourceId, ct);
        var bytes = await RenderAsync(ingredients, isVertical, ct);
        return await mediator.Send(new UploadImageCommand(bytes, purpose), ct);
    }

    private record Ingredients(
        int? PrimaryLogoImageId,
        int? SecondaryLogoImageId,
        string? PrimaryColorHex,
        string? SecondaryColorHex,
        int? BadgeImageId,
        int? ProviderLogoImageId);

    private async Task<Ingredients> ResolveIngredientsAsync(Guid sourceId, CancellationToken ct)
    {
        var ev = await workspace.LoadById<SportingEvent, Guid>(sourceId, ct);
        var league = await workspace.LoadById<League, int>(ev.LeagueId, ct);
        var home = ev.HomeTeamId is { } h ? await workspace.LoadById<Team, int>(h, ct) : null;
        var away = ev.AwayTeamId is { } a ? await workspace.LoadById<Team, int>(a, ct) : null;
        var broadcaster = ev.BroadcasterId is { } b ? await workspace.LoadById<Broadcaster, int>(b, ct) : null;
        var logoSource = await ResolveCarrierLogoSourceAsync(broadcaster, ct);

        return new Ingredients(
            home?.SelectedLogo()?.ImageId,
            away?.SelectedLogo()?.ImageId,
            home?.PrimaryColorHex,
            away?.PrimaryColorHex,
            league.SelectedLogo(ev.Variant)?.ImageId,
            logoSource?.SelectedLogo()?.ImageId);
    }

    private async Task<Broadcaster?> ResolveCarrierLogoSourceAsync(Broadcaster? broadcaster, CancellationToken ct)
    {
        if (broadcaster is null)
            return null;

        if (broadcaster.IptvGuideNumber is { Length: > 0 })
            return broadcaster;

        if (broadcaster.MapsToBroadcasterId is not { } targetId)
            return broadcaster;

        return await workspace.LoadById<Broadcaster, int>(targetId, ct);
    }

    private async Task<byte[]?> LoadBytesAsync(int? imageId, CancellationToken ct)
    {
        if (imageId is not { } id)
            return null;

        var image = await readModel.SingleOrDefault(new ImageByIdSpec(id), ct);
        return image?.Bytes;
    }

    private async Task<byte[]> RenderAsync(Ingredients ingredients, bool isVertical, CancellationToken ct)
    {
        var primaryLogo = await LoadBytesAsync(ingredients.PrimaryLogoImageId, ct);
        var secondaryLogo = await LoadBytesAsync(ingredients.SecondaryLogoImageId, ct);
        var badgeLogo = await LoadBytesAsync(ingredients.BadgeImageId, ct);
        var providerLogo = await LoadBytesAsync(ingredients.ProviderLogoImageId, ct);

        int width = isVertical ? 1000 : 1920;
        int height = isVertical ? 1400 : 1080;

        var color1 = ParseColorOrDefault(ingredients.PrimaryColorHex, Rgba32.ParseHex("#222222"));
        var color2 = ParseColorOrDefault(ingredients.SecondaryColorHex, Rgba32.ParseHex("#333333"));

        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.Fill(Color.Black));
        image.Mutate(ctx => DrawDiagonalSplit(ctx, width, height, isVertical, color1, color2));

        if (primaryLogo is { Length: > 0 } homeLogo)
        {
            using var logo1 = await Image.LoadAsync<Rgba32>(new MemoryStream(homeLogo), ct);
            DrawTeamLogo(image, logo1, isVertical, side: 0);
        }

        if (secondaryLogo is { Length: > 0 } awayLogo)
        {
            using var logo2 = await Image.LoadAsync<Rgba32>(new MemoryStream(awayLogo), ct);
            DrawTeamLogo(image, logo2, isVertical, side: 1);
        }

        if (badgeLogo is { Length: > 0 } leagueLogo)
        {
            using var league = await Image.LoadAsync<Rgba32>(new MemoryStream(leagueLogo), ct);
            league.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(isVertical ? 140 : 150, 0),
                Mode = ResizeMode.Max
            }));
            image.Mutate(ctx => ctx.DrawImage(league, new Point(isVertical ? 60 : 40, isVertical ? 40 : 40), 1f));
        }

        if (providerLogo is { Length: > 0 } streamingLogo)
        {
            using var streaming = await Image.LoadAsync<Rgba32>(new MemoryStream(streamingLogo), ct);
            streaming.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(180, 0), Mode = ResizeMode.Max }));
            var xPos = width - streaming.Width - (isVertical ? 60 : 40);
            var yPos = height - streaming.Height - 15;
            image.Mutate(ctx => ctx.DrawImage(streaming, new Point(xPos, yPos), 1f));
        }

        using var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = 95 }, ct);
        return output.ToArray();
    }

    private static void DrawTeamLogo(Image<Rgba32> canvas, Image<Rgba32> logo, bool isVertical, int side)
    {
        int w = canvas.Width;
        int h = canvas.Height;

        int targetW = isVertical ? w - 300 : (w / 2) - 300;
        int targetH = isVertical ? (h / 2) - 300 : h - 300;

        logo.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(targetW, targetH), Mode = ResizeMode.Max }));

        int x = isVertical
            ? (w - logo.Width) / 2
            : side == 0 ? (w / 4) - (logo.Width / 2) : (3 * w / 4) - (logo.Width / 2);

        int y = isVertical
            ? side == 0 ? (h / 4) - (logo.Height / 2) : (3 * h / 4) - (logo.Height / 2)
            : (h - logo.Height) / 2;

        canvas.Mutate(ctx => ctx.DrawImage(logo, new Point(x, y), 1f));
    }

    private static void DrawDiagonalSplit(IImageProcessingContext ctx, int width, int height, bool isVertical, Rgba32 color1, Rgba32 color2)
    {
        var pb1 = new PathBuilder();
        var pb2 = new PathBuilder();

        if (isVertical)
        {
            float tiltOffset = width * 0.268f;
            float midpoint = height / 2f;

            pb1.AddLines(
                new PointF(0, 0),
                new PointF(width, 0),
                new PointF(width, midpoint - tiltOffset / 2),
                new PointF(0, midpoint + tiltOffset / 2));

            pb2.AddLines(
                new PointF(0, midpoint + tiltOffset / 2),
                new PointF(width, midpoint - tiltOffset / 2),
                new PointF(width, height),
                new PointF(0, height));

            ctx.Fill(color1, pb1.Build());
            ctx.Fill(color2, pb2.Build());

            var borderPath1 = new PathBuilder();
            borderPath1.AddLines(
                new PointF(0, midpoint + tiltOffset / 2 - 25),
                new PointF(width, midpoint - tiltOffset / 2 - 25),
                new PointF(width, midpoint - tiltOffset / 2),
                new PointF(0, midpoint + tiltOffset / 2));

            var borderPath2 = new PathBuilder();
            borderPath2.AddLines(
                new PointF(0, midpoint + tiltOffset / 2),
                new PointF(width, midpoint - tiltOffset / 2),
                new PointF(width, midpoint - tiltOffset / 2 + 25),
                new PointF(0, midpoint + tiltOffset / 2 + 25));

            ctx.Fill(LightenColor(color1, 0.2f), borderPath1.Build());
            ctx.Fill(LightenColor(color2, 0.2f), borderPath2.Build());
        }
        else
        {
            float tiltOffset = height * 0.268f;
            float midpoint = width / 2f;

            pb1.AddLines(
                new PointF(0, 0),
                new PointF(midpoint + tiltOffset / 2, 0),
                new PointF(midpoint - tiltOffset / 2, height),
                new PointF(0, height));

            pb2.AddLines(
                new PointF(midpoint + tiltOffset / 2, 0),
                new PointF(width, 0),
                new PointF(width, height),
                new PointF(midpoint - tiltOffset / 2, height));

            ctx.Fill(color1, pb1.Build());
            ctx.Fill(color2, pb2.Build());

            var borderPath1 = new PathBuilder();
            borderPath1.AddLines(
                new PointF(midpoint + tiltOffset / 2 - 25, 0),
                new PointF(midpoint + tiltOffset / 2, 0),
                new PointF(midpoint - tiltOffset / 2, height),
                new PointF(midpoint - tiltOffset / 2 - 25, height));

            var borderPath2 = new PathBuilder();
            borderPath2.AddLines(
                new PointF(midpoint + tiltOffset / 2, 0),
                new PointF(midpoint + tiltOffset / 2 + 25, 0),
                new PointF(midpoint - tiltOffset / 2 + 25, height),
                new PointF(midpoint - tiltOffset / 2, height));

            ctx.Fill(LightenColor(color1, 0.05f), borderPath1.Build());
            ctx.Fill(LightenColor(color2, 0.05f), borderPath2.Build());
        }
    }

    private static Rgba32 ParseColorOrDefault(string? hex, Rgba32 fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { return Rgba32.ParseHex(hex.Trim().TrimStart('#')); }
        catch { return fallback; }
    }

    private static Rgba32 LightenColor(Rgba32 color, float factor) => new(
        (byte)Math.Min(255, color.R + (255 - color.R) * factor),
        (byte)Math.Min(255, color.G + (255 - color.G) * factor),
        (byte)Math.Min(255, color.B + (255 - color.B) * factor),
        color.A);
}
