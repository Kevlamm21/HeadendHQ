using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Settings;

public class GlobalSettingsSpec : ISpecification<GlobalSettings>
{
    public IQueryable<GlobalSettings> Apply(IQueryable<GlobalSettings> q) => q;
}
