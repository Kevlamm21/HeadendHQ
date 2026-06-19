using HeadendHQ.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HeadendHQ.SixLabors;

public class ImageNormalizer : IImageNormalizer
{
    public Task<byte[]> NormalizeTeamLogoAsync(byte[] input, CancellationToken ct = default) =>
        NormalizePngAsync(input, width: 800, height: 800, ct);

    public Task<byte[]> NormalizeLeagueLogoAsync(byte[] input, CancellationToken ct = default) =>
        NormalizePngAsync(input, width: 300, height: 300, ct);

    public Task<byte[]> NormalizeStreamingLogoAsync(byte[] input, CancellationToken ct = default) =>
        NormalizePngAsync(input, width: 300, height: 300, ct);

    public Task<byte[]> NormalizeWordMarkAsync(byte[] input, CancellationToken ct = default) =>
        NormalizePngAsync(input, width: 800, height: 400, ct);

    public Task<byte[]> NormalizePosterAsync(byte[] input, CancellationToken ct = default) =>
        NormalizeJpegAsync(input, width: 1000, height: 1400, ct);

    public Task<byte[]> NormalizeBackgroundAsync(byte[] input, CancellationToken ct = default) =>
        NormalizeJpegAsync(input, width: 1920, height: 1080, ct);

    private static async Task<byte[]> NormalizePngAsync(byte[] input, int width, int height, CancellationToken ct)
    {
        using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(input), ct);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Crop
        }));

        using var output = new MemoryStream();
        await image.SaveAsync(output, new PngEncoder(), ct);
        return output.ToArray();
    }

    private static async Task<byte[]> NormalizeJpegAsync(byte[] input, int width, int height, CancellationToken ct)
    {
        using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(input), ct);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Crop
        }));

        using var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = 95 }, ct);
        return output.ToArray();
    }
}
