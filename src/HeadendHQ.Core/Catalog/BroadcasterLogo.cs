using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog;

public class BroadcasterLogo
{
    private BroadcasterLogo() { }

    internal BroadcasterLogo(string variant, ImageRef image)
    {
        Variant = variant;
        Image = image;
    }

    public int Id { get; init; }
    public int BroadcasterId { get; private set; }

    /// <summary>ESPN publishes <c>default</c> and often <c>dark</c>; nothing else exists.</summary>
    public string Variant { get; private set; } = LogoVariants.Default;
    public ImageRef Image { get; private set; } = ImageRef.Empty();

    internal void PointAt(string sourceKey, string sourceUrl, DateTimeOffset? sourceUpdatedAtUtc) =>
        Image.PointAt(sourceKey, sourceUrl, sourceUpdatedAtUtc);
}
