using HeadendHQ.Core.Shared;

namespace HeadendHQ.DummyVideo.Settings;

public class DummyVideoSettingsSpec : ISpecification<DummyVideoSettings>
{
    public IQueryable<DummyVideoSettings> Apply(IQueryable<DummyVideoSettings> q) => q;
}
