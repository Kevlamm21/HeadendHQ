using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.HdHomerun.Settings;

public record GetHdHomerunSettingsQuery : IQuery<HdHomerunSettings>;

public class GetHdHomerunSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetHdHomerunSettingsQuery, HdHomerunSettings>
{
    public async ValueTask<HdHomerunSettings> Handle(GetHdHomerunSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new HdHomerunSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("HdHomerunSettings not found.");
    }
}
