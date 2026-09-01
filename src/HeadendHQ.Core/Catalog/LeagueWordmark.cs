using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog;

/// <summary>
/// A Jellyfin clearlogo. ESPN has no equivalent asset, so these are upload-only; a league without
/// one simply produces VOD folders with no clearlogo.
/// </summary>
public class LeagueWordmark
{
    private LeagueWordmark() { }

    internal LeagueWordmark(string variant, ImageRef image)
    {
        Variant = variant;
        Image = image;
    }

    public int Id { get; init; }
    public int LeagueId { get; private set; }
    public string Variant { get; private set; } = LogoVariants.Default;
    public ImageRef Image { get; private set; } = ImageRef.Empty();
}
