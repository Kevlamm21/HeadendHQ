using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog.Leagues;

/// <summary>
/// A Jellyfin clearlogo. ESPN has no equivalent asset, so these are upload-only; a league without
/// one simply produces VOD folders with no clearlogo. <see cref="Label"/> is therefore always null.
/// </summary>
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

    public void PointAt(int imageId, ImageOrigin origin) => ImageId = imageId;
}
