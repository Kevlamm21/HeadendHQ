using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Media.Specifications;

public class ImageByHashSpec(string sha256) : ISpecification<Image>
{
    public IQueryable<Image> Apply(IQueryable<Image> queryable) => queryable.Where(i => i.Sha256 == sha256);
}

/// <summary>Image metadata without the bytes, so listings never serialize a BLOB.</summary>
public record ImageSummary(int Id, string Sha256, string ContentType, int Width, int Height, ImageOrigin Origin);

public class ImageSummariesSpec : ISpecification<Image, ImageSummary>
{
    public IQueryable<ImageSummary> Apply(IQueryable<Image> queryable) =>
        queryable.Select(i => new ImageSummary(i.Id, i.Sha256, i.ContentType, i.Width, i.Height, i.Origin));
}

public class ImageByIdSpec(int id) : ISpecification<Image>
{
    public IQueryable<Image> Apply(IQueryable<Image> queryable) => queryable.Where(i => i.Id == id);
}

/// <summary>
/// The bytes we already hold for an upstream URL. Checked before a download, which is what keeps a
/// player's headshot to one request ever rather than one per game.
/// <para>
/// Keyed by purpose as well as address, because normalization is chosen by purpose: the same URL
/// fetched as a headshot and as a team logo produces different pixels, so they are different rows.
/// </para>
/// </summary>
public class ImageBySourceUrlSpec(string sourceUrl, ImagePurpose purpose) : ISpecification<Image>
{
    public IQueryable<Image> Apply(IQueryable<Image> queryable) =>
        queryable.Where(i => i.SourceUrl == sourceUrl && i.Purpose == purpose);
}

/// <summary>
/// The same question as <see cref="ImageBySourceUrlSpec"/>, answered without the BLOB. This runs
/// once per billed player per event, and all the caller wants back is an id.
/// </summary>
public class ImageIdBySourceUrlSpec(string sourceUrl, ImagePurpose purpose) : ISpecification<Image, int>
{
    public IQueryable<int> Apply(IQueryable<Image> queryable) =>
        queryable.Where(i => i.SourceUrl == sourceUrl && i.Purpose == purpose).Select(i => i.Id);
}

/// <summary>
/// Every image of one kind, optionally narrowed to a league. The whole point of recording a purpose:
/// wiping last season's headshots is this, not a walk of the catalog.
/// </summary>
public class ImagesByPurposeSpec(ImagePurpose purpose, int? leagueId) : ISpecification<Image>
{
    public IQueryable<Image> Apply(IQueryable<Image> queryable) =>
        queryable.Where(i => i.Purpose == purpose && (leagueId == null || i.LeagueId == leagueId));
}
