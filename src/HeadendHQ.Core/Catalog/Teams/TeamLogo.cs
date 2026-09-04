using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog.Teams;

public class TeamLogo : ILogo
{
    private TeamLogo() { }

    internal TeamLogo(string variant, string? label, int imageId, ImageOrigin origin)
    {
        Variant = variant;
        Label = label;
        ImageId = imageId;
        Origin = origin;
    }

    public int Id { get; init; }
    public int TeamId { get; private set; }

    /// <summary>Teams have no editions of their own; carried so the collection matches a league's.</summary>
    public string Variant { get; private set; } = LogoVariants.Default;

    public string? Label { get; private set; } = LogoRels.Default;
    public ImageOrigin Origin { get; private set; }
    public int ImageId { get; private set; }

    public void PointAt(int imageId, ImageOrigin origin)
    {
        ImageId = imageId;
        Origin = origin;
    }
}
