using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog.Broadcasters;

public class BroadcasterLogo : ILogo
{
    private BroadcasterLogo() { }

    internal BroadcasterLogo(string variant, string? label, int imageId, ImageOrigin origin)
    {
        Variant = variant;
        Label = label;
        ImageId = imageId;
        Origin = origin;
    }

    public int Id { get; init; }
    public int BroadcasterId { get; private set; }

    public string Variant { get; private set; } = LogoVariants.Default;

    public string? Label { get; private set; } = LogoRels.Default;

    public ImageOrigin Origin { get; private set; }
    public int ImageId { get; private set; }
    public bool IsSelected { get; private set; }

    public void PointAt(int imageId, ImageOrigin origin)
    {
        ImageId = imageId;
        Origin = origin;
    }

    public void Select(bool selected) => IsSelected = selected;
}
