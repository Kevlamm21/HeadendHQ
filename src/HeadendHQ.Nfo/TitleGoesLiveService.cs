using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Nfo;

public class TitleGoesLiveService(
    IMediator mediator,
    IReadModel readModel,
    ILogger<TitleGoesLiveService> logger)
{
    public async Task MarkAsLiveAsync(Guid titleId, CancellationToken ct = default)
    {
        var exists = await readModel.Count(new EntityByIdSpecification<Title, Guid>(titleId), ct) > 0;

        if (!exists)
        {
            logger.LogWarning("Title {Id} no longer exists. Skipping go-live.", titleId);
            return;
        }

        await mediator.Send(new UpdateTitleCommand(titleId, new UpdateTitleRequest { IsLive = true }), ct);
    }
}
