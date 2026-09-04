using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.CommandHandlers;

public record GetCatalogSyncStateQuery : IQuery<CatalogSyncState?>;

public class GetCatalogSyncStateHandler(IReadModel readModel)
    : IQueryHandler<GetCatalogSyncStateQuery, CatalogSyncState?>
{
    public async ValueTask<CatalogSyncState?> Handle(GetCatalogSyncStateQuery query, CancellationToken ct) =>
        await readModel.SingleOrDefault(new CatalogSyncStateSpec(), ct);
}
