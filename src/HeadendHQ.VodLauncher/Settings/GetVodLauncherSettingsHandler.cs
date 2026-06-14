using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.VodLauncher.Settings;

public record GetVodLauncherSettingsQuery : IQuery<VodLauncherSettings>;

public class GetVodLauncherSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetVodLauncherSettingsQuery, VodLauncherSettings>
{
    public async ValueTask<VodLauncherSettings> Handle(GetVodLauncherSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new VodLauncherSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("VodLauncherSettings not found.");
    }
}
