using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleGoesLiveService(IMediator mediator)
{
    public async Task MarkAsLiveAsync(Guid titleId, CancellationToken ct = default)
    {
        await mediator.Send(new UpdateTitleCommand(titleId, new UpdateTitleRequest { IsLive = true }), ct);
    }
}
