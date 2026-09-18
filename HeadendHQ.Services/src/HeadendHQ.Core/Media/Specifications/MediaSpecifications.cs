using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Media.Specifications;

public class ImageByHashSpec(string sha256) : ISpecification<Image>
{
    public IQueryable<Image> Apply(IQueryable<Image> queryable) => queryable.Where(i => i.Sha256 == sha256);
}

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

public class ImageBySourceUrlSpec(string sourceUrl, ImagePurpose purpose) : ISpecification<Image>
{
    public IQueryable<Image> Apply(IQueryable<Image> queryable) =>
        queryable.Where(i => i.SourceUrl == sourceUrl && i.Purpose == purpose);
}

public class ImageIdBySourceUrlSpec(string sourceUrl, ImagePurpose purpose) : ISpecification<Image, int>
{
    public IQueryable<int> Apply(IQueryable<Image> queryable) =>
        queryable.Where(i => i.SourceUrl == sourceUrl && i.Purpose == purpose).Select(i => i.Id);
}

public class ImagesByPurposeSpec(ImagePurpose purpose, int? leagueId) : ISpecification<Image>
{
    public IQueryable<Image> Apply(IQueryable<Image> queryable) =>
        queryable.Where(i => i.Purpose == purpose && (leagueId == null || i.LeagueId == leagueId));
}
