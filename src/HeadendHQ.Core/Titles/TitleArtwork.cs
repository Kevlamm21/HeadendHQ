namespace HeadendHQ.Core.Titles;

/// <summary>
/// Everything the artwork composer needs, already resolved to image ids and colours.
/// <para>
/// This exists so <c>ImageCreationService</c> never has to know what a league or a broadcaster is.
/// Whatever produced the title — a sporting event today, a video game later — does the lookups once
/// and leaves behind a flat set of ingredients.
/// </para>
/// </summary>
public class TitleArtwork
{
    public int? PrimaryLogoImageId { get; private set; }
    public int? SecondaryLogoImageId { get; private set; }

    /// <summary>Hex without a leading '#'. Drives the split-colour card.</summary>
    public string? PrimaryColorHex { get; private set; }
    public string? SecondaryColorHex { get; private set; }

    /// <summary>The competition mark: a league logo for a game, whatever fits for other types.</summary>
    public int? BadgeImageId { get; private set; }

    /// <summary>The provider mark, shown as the "where to watch" corner.</summary>
    public int? ProviderLogoImageId { get; private set; }

    /// <summary>The Jellyfin clearlogo. Upload-only, so frequently absent.</summary>
    public int? WordmarkImageId { get; private set; }

    public static TitleArtwork Empty() => new();

    public static TitleArtwork Create(
        int? primaryLogoImageId, int? secondaryLogoImageId,
        string? primaryColorHex, string? secondaryColorHex,
        int? badgeImageId, int? providerLogoImageId, int? wordmarkImageId) =>
        new()
        {
            PrimaryLogoImageId = primaryLogoImageId,
            SecondaryLogoImageId = secondaryLogoImageId,
            PrimaryColorHex = primaryColorHex,
            SecondaryColorHex = secondaryColorHex,
            BadgeImageId = badgeImageId,
            ProviderLogoImageId = providerLogoImageId,
            WordmarkImageId = wordmarkImageId,
        };
}
