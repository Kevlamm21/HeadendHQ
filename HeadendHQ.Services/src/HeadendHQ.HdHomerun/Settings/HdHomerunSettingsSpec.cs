using HeadendHQ.Core.Shared;

namespace HeadendHQ.HdHomerun.Settings;

public class HdHomerunSettingsSpec : ISpecification<HdHomerunSettings>
{
    public IQueryable<HdHomerunSettings> Apply(IQueryable<HdHomerunSettings> q) => q;
}
