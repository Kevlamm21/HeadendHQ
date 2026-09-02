using HeadendHQ.Core;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HeadendHQ.SixLabors;

public class ImageCreationService(IReadModel readModel) : IImageCreationService
{
    public async Task CreatePosterAsync(Title title, CancellationToken ct = default)
    {
        await RenderAsync(
            System.IO.Path.Combine(GetFolder(title), TitleArtworkFiles.Poster(title.Name)),
            title, isVertical: true, ct);
    }

    public async Task CreateThumbAsync(Title title, CancellationToken ct = default)
    {
        await RenderAsync(
            System.IO.Path.Combine(GetFolder(title), TitleArtworkFiles.Thumb(title.Name)),
            title, isVertical: false, ct);
    }

    public async Task CreateBackdropAsync(Title title, CancellationToken ct = default)
    {
        await RenderAsync(
            System.IO.Path.Combine(GetFolder(title), TitleArtworkFiles.Backdrop(title.Name)),
            title, isVertical: false, ct);
    }

    public async Task CreateClearLogoAsync(Title title, CancellationToken ct = default)
    {
        if (await LoadBytesAsync(title.Artwork.WordmarkImageId, ct) is not { Length: > 0 } logoData)
            return;

        await File.WriteAllBytesAsync(
            System.IO.Path.Combine(GetFolder(title), TitleArtworkFiles.ClearLogo(title.Name)), logoData, ct);
    }

    /// <summary>
    /// Every ingredient is already an image id on the title, so composing artwork is a handful of
    /// primary-key reads. Nothing here knows what a team, a league or a broadcaster is.
    /// </summary>
    private async Task<byte[]?> LoadBytesAsync(int? imageId, CancellationToken ct)
    {
        if (imageId is not { } id)
            return null;

        var image = await readModel.SingleOrDefault(new ImageByIdSpec(id), ct);
        return image?.Bytes;
    }

    private static string GetFolder(Title title) =>
        title.VodLauncherPath ?? throw new InvalidOperationException(
            $"Title '{title.Name}' ({title.Id}) has no VodLauncherPath — cannot write artwork.");

    private async Task RenderAsync(string outputPath, Title title, bool isVertical, CancellationToken ct)
    {
        var artwork = title.Artwork;

        var primaryLogo = await LoadBytesAsync(artwork.PrimaryLogoImageId, ct);
        var secondaryLogo = await LoadBytesAsync(artwork.SecondaryLogoImageId, ct);
        var badgeLogo = await LoadBytesAsync(artwork.BadgeImageId, ct);
        var providerLogo = await LoadBytesAsync(artwork.ProviderLogoImageId, ct);

        int width = isVertical ? 1000 : 1920;
        int height = isVertical ? 1400 : 1080;

        var color1 = ParseColorOrDefault(artwork.PrimaryColorHex, Rgba32.ParseHex("#222222"));
        var color2 = ParseColorOrDefault(artwork.SecondaryColorHex, Rgba32.ParseHex("#333333"));

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

        await image.SaveAsync(outputPath, new JpegEncoder { Quality = 95 }, ct);
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
