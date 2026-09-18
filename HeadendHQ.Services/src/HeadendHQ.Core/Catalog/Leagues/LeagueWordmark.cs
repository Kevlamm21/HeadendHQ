using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog.Leagues;

public class LeagueWordmark : ILogo
{
    private LeagueWordmark() { }

    internal LeagueWordmark(string variant, int imageId)
    {
        Variant = variant;
        ImageId = imageId;
    }

    public int Id { get; init; }
    public int LeagueId { get; private set; }
    public string Variant { get; private set; } = LogoVariants.Default;
    public string? Label => null;
    public ImageOrigin Origin => ImageOrigin.Manual;
    public int ImageId { get; private set; }
    public bool IsSelected => true;

    public void PointAt(int imageId, ImageOrigin origin) => ImageId = imageId;

    public void Select(bool selected) { }
}
