using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog;

public class LeagueLogo
{
    private LeagueLogo() { }

    internal LeagueLogo(string variant, string rel, ImageRef image)
    {
        Variant = variant;
        Rel = rel;
        Image = image;
    }

    public int Id { get; init; }
    public int LeagueId { get; private set; }
    public string Variant { get; private set; } = LogoVariants.Default;
    public string Rel { get; private set; } = LogoRels.Default;
    public ImageRef Image { get; private set; } = ImageRef.Empty();

    internal void PointAt(string sourceKey, string sourceUrl, DateTimeOffset? sourceUpdatedAtUtc) =>
        Image.PointAt(sourceKey, sourceUrl, sourceUpdatedAtUtc);
}
