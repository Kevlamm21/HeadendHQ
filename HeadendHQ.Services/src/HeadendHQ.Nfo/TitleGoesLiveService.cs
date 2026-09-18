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
    public async Task MarkAsLiveAsync(Guid titleId, DateTime scheduledStartUtc, CancellationToken ct = default)
    {
        var title = await readModel.SingleOrDefault(new EntityByIdSpecification<Title, Guid>(titleId), ct);

        if (title is null)
        {
            logger.LogWarning("Title {Id} no longer exists. Skipping go-live.", titleId);
            return;
        }

        if (title.IsLive)
            return;

        if (title.StartUtc != scheduledStartUtc)
        {
            logger.LogInformation("Title {Id} ({Name}) was rescheduled since this go-live was queued. Skipping.",
                title.Id, title.Name);
            return;
        }

        await mediator.Send(new UpdateTitleCommand(titleId, new UpdateTitleRequest { IsLive = true }), ct);
    }
}
