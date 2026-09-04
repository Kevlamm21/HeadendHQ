using HeadendHQ.Core.Media;

namespace HeadendHQ.Core.Catalog;

/// <summary>
/// One stored mark belonging to a catalog record. Teams, leagues and broadcasters each own their own
/// collection, but the shape is identical so the same add/replace/lookup logic — and, later, the same
/// front end — works against all of them.
/// <para>
/// A logo always has bytes. There is no "discovered but not downloaded" state: a row exists because
/// something decided the image was worth holding, which is what lets a picker render every row it
/// finds.
/// </para>
/// </summary>
public interface ILogo
{
    int Id { get; }

    /// <summary>
    /// Which edition of the owner's identity this belongs to — see <see cref="LogoVariants"/>. Only
    /// leagues have more than <see cref="LogoVariants.Default"/> today; teams and broadcasters carry
    /// it so the collection behaves the same everywhere.
    /// </summary>
    string Variant { get; }

    /// <summary>
    /// The source's own name for this mark — see <see cref="LogoRels"/>. Null for a hand upload,
    /// which answers to no upstream token and so is never matched by a refresh.
    /// </summary>
    string? Label { get; }

    ImageOrigin Origin { get; }

    int ImageId { get; }

    /// <summary>Repoints at different bytes. Used when a refresh finds the mark has changed.</summary>
    void PointAt(int imageId, ImageOrigin origin);
}

/// <summary>
/// The collection behaviour every catalog record shares. A static helper rather than a base class:
/// these are owned collections on three different aggregates, and an EF inheritance hierarchy across
/// owned types costs more than the handful of duplicated properties it would save.
/// </summary>
public static class Logos
{
    public static T? Find<T>(IEnumerable<T> logos, string variant, string? label) where T : ILogo =>
        logos.FirstOrDefault(logo => logo.Variant == variant && logo.Label == label);

    /// <summary>
    /// Records bytes against a slot, replacing whatever was there.
    /// <para>
    /// A hand upload is never overwritten by a refresh — that is the whole promise of uploading one —
    /// so a <see cref="ImageOrigin.Fetched"/> write onto a <see cref="ImageOrigin.Manual"/> row is
    /// dropped rather than applied.
    /// </para>
    /// </summary>
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
