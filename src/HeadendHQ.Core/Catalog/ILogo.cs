using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog;

public interface ILogo
{
    int Id { get; }

    string Variant { get; }

    string? Label { get; }

    ImageOrigin Origin { get; }

    int ImageId { get; }

    void PointAt(int imageId, ImageOrigin origin);
}

public static class Logos
{
    public static T? Find<T>(IEnumerable<T> logos, string variant, string? label) where T : ILogo =>
        logos.FirstOrDefault(logo => logo.Variant == variant && logo.Label == label);

    public static T Upsert<T>(
        IList<T> logos, string variant, string? label, int imageId, ImageOrigin origin, Func<T> create)
        where T : ILogo
    {
        if (Find(logos, variant, label) is { } existing)
        {
            if (origin is ImageOrigin.Fetched && existing.Origin is ImageOrigin.Manual)
                return existing;

            if (existing.ImageId != imageId)
                existing.PointAt(imageId, origin);

            return existing;
        }

        var logo = create();
        logos.Add(logo);
        return logo;
    }
}
