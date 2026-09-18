using HeadendHQ.Core.Shared;

namespace HeadendHQ.VodLauncher.Settings;

public class VodLauncherSettingsSpec : ISpecification<VodLauncherSettings>
{
    public IQueryable<VodLauncherSettings> Apply(IQueryable<VodLauncherSettings> q) => q;
}
