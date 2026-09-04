using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Settings;

public record GetGlobalSettingsQuery : IQuery<GlobalSettings>;

public class GetGlobalSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetGlobalSettingsQuery, GlobalSettings>
{
    public async ValueTask<GlobalSettings> Handle(GetGlobalSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new GlobalSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("GlobalSettings not found.");
    }
}
