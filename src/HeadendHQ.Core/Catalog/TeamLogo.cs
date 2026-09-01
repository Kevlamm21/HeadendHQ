using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog;

public class TeamLogo
{
    private TeamLogo() { }

    internal TeamLogo(string rel, ImageRef image)
    {
        Rel = rel;
        Image = image;
    }

    public int Id { get; init; }
    public int TeamId { get; private set; }
    public string Rel { get; private set; } = LogoRels.Default;
    public ImageRef Image { get; private set; } = ImageRef.Empty();

    internal void PointAt(string sourceKey, string sourceUrl, DateTimeOffset? sourceUpdatedAtUtc) =>
        Image.PointAt(sourceKey, sourceUrl, sourceUpdatedAtUtc);
}
