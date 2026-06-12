using HeadendHQ.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HeadendHQ.SixLabors;

public class LogoNormalizer : ILogoNormalizer
{
    public Task<byte[]> NormalizeTeamLogoAsync(byte[] input, CancellationToken ct = default) =>
        NormalizeAsync(input, width: 800, height: 800, ct);

    public Task<byte[]> NormalizeLeagueLogoAsync(byte[] input, CancellationToken ct = default) =>
        NormalizeAsync(input, width: 300, height: 300, ct);

    public Task<byte[]> NormalizeStreamingLogoAsync(byte[] input, CancellationToken ct = default) =>
        NormalizeAsync(input, width: 300, height: 300, ct);

    public Task<byte[]> NormalizeWordMarkAsync(byte[] input, CancellationToken ct = default) =>
        NormalizeAsync(input, width: 800, height: 400, ct);

    private static async Task<byte[]> NormalizeAsync(byte[] input, int width, int height, CancellationToken ct)
    {
        using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(input), ct);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Pad,
            PadColor = Color.Transparent
        }));

        using var output = new MemoryStream();
        await image.SaveAsync(output, new PngEncoder(), ct);
        return output.ToArray();
    }
}
