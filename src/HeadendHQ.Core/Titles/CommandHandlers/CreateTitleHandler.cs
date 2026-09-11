using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record CreateTitleCommand(TitleRequest Request) : ICommand<Title>;

public class CreateTitleHandler(IWorkspace workspace)
    : ICommandHandler<CreateTitleCommand, Title>
{
    public async ValueTask<Title> Handle(CreateTitleCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var existing = (await workspace.Load(new TitleByNameStartSpec(request.Name, request.StartUtc), ct))
            .FirstOrDefault();

        if (existing is not null)
        {
            existing.Update(new UpdateTitleRequest
            {
                Name = request.Name,
                Type = request.Type,
                SourceId = request.SourceId,
                LaunchSlug = request.LaunchSlug,
                EventUrl = request.EventUrl,
                StartUtc = request.StartUtc,
                EndUtc = request.EndUtc ?? request.StartUtc?.AddHours(3),
                Plot = request.Plot,
                Tagline = request.Tagline,
                Studio = request.Studio,
                Genres = request.Genres,
                Sets = request.Sets,
                ContentRating = request.ContentRating,
                UniqueId = request.UniqueId,
                Cast = request.Cast,
            });
            return existing;
        }

        var title = new Title(request);
        workspace.Add(title);
        return title;
    }
}
