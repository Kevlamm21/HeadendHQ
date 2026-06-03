using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.DummyVideo.Settings;

public record GetDummyVideoSettingsQuery : IQuery<DummyVideoSettings>;

public class GetDummyVideoSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetDummyVideoSettingsQuery, DummyVideoSettings>
{
    public async ValueTask<DummyVideoSettings> Handle(GetDummyVideoSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new DummyVideoSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("DummyVideoSettings not found.");
    }
}
