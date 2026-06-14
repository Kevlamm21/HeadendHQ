using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Assets.Specifications;

public class StreamingServiceAssetByServiceSpec(StreamingService service) : ISpecification<StreamingServiceAsset>
{
    public IQueryable<StreamingServiceAsset> Apply(IQueryable<StreamingServiceAsset> queryable) =>
        queryable.Where(a => a.Service == service);
}
