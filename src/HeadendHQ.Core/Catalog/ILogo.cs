using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog;

public interface ILogo
{
    int Id { get; }

    string Variant { get; }

    string? Label { get; }

    ImageOrigin Origin { get; }

    int ImageId { get; }

    bool IsSelected { get; }

    void PointAt(int imageId, ImageOrigin origin);

    void Select(bool selected);
}

public record FetchedLogo(string Label, int ImageId);

public static class Logos
{
    public static T? Find<T>(IEnumerable<T> logos, string variant, string? label) where T : ILogo =>
        logos.FirstOrDefault(logo => logo.Variant == variant && logo.Label == label);

    public static bool HasFetched<T>(IEnumerable<T> logos) where T : ILogo =>
        logos.Any(logo => logo.Origin is ImageOrigin.Fetched);

    public static IReadOnlyList<int> StoreFetched<T>(
        IList<T> logos, string variant, LogoDownload download, LogoPolicy policy,
        Func<FetchedLogo, T> create)
        where T : ILogo
    {
        if (download.Stored.Count == 0)
            return [];

        var dropped = new List<int>();

        foreach (var logo in download.Stored)
        {
            if (Find(logos, variant, logo.Label) is { } existing)
            {
                if (existing.ImageId != logo.ImageId)
                {
                    dropped.Add(existing.ImageId);
                    existing.PointAt(logo.ImageId, ImageOrigin.Fetched);
                }
            }
            else
            {
                logos.Add(create(logo));
            }
        }

        var keep = download.Wanted.ToHashSet();

        foreach (var stale in logos
                     .Where(l => l.Variant == variant && l.Origin is ImageOrigin.Fetched
                         && (l.Label is null || !keep.Contains(l.Label)))
                     .ToList())
        {
            dropped.Add(stale.ImageId);
            logos.Remove(stale);
        }

        EnsureSelected(logos, variant, policy);

        return dropped;
    }

    public static T AddUpload<T>(IList<T> logos, string variant, int imageId, Func<T> create) where T : ILogo
    {
        if (logos.FirstOrDefault(l => l.Variant == variant && l.ImageId == imageId) is { } existing)
            return existing;

        var logo = create();
        logos.Add(logo);
        return logo;
    }

    public static T Select<T>(IList<T> logos, int logoId) where T : ILogo
    {
        var chosen = logos.FirstOrDefault(l => l.Id == logoId)
            ?? throw new InvalidOperationException($"Logo {logoId} does not belong to this item.");

        foreach (var logo in logos.Where(l => l.Variant == chosen.Variant))
            logo.Select(logo.Id == chosen.Id);

        return chosen;
    }

    public static int Remove<T>(IList<T> logos, int logoId, LogoPolicy policy) where T : ILogo
    {
        var logo = logos.FirstOrDefault(l => l.Id == logoId)
            ?? throw new InvalidOperationException($"Logo {logoId} does not belong to this item.");

        if (logo.Origin is not ImageOrigin.Manual)
            throw new InvalidOperationException(
                "Only uploaded logos can be deleted; refresh replaces the ones fetched from the source.");

        logos.Remove(logo);
        EnsureSelected(logos, logo.Variant, policy);

        return logo.ImageId;
    }

    public static T? Selected<T>(IEnumerable<T> logos, string variant, LogoPolicy policy) where T : ILogo
    {
        var inVariant = logos.Where(l => l.Variant == variant).ToList();

        return inVariant.FirstOrDefault(l => l.IsSelected)
            ?? (policy.DefaultLabel(inVariant.Select(l => l.Label)) is { } label
                ? inVariant.First(l => string.Equals(l.Label, label, StringComparison.OrdinalIgnoreCase))
                : inVariant.FirstOrDefault());
    }

    public static void EnsureSelected<T>(IList<T> logos, string variant, LogoPolicy policy) where T : ILogo
    {
        if (logos.Any(l => l.Variant == variant && l.IsSelected))
            return;

        Selected(logos, variant, policy)?.Select(true);
    }
}
