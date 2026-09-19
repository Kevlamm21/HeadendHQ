using HeadendHQ.Core.Catalog.Broadcasters.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record ResolveBroadcasterCommand(
    string ExternalId, string Slug, string Name, BroadcasterKind Kind) : ICommand<Broadcaster>;

public class ResolveBroadcasterHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IBroadcasterCatalogSource source)
    : ICommandHandler<ResolveBroadcasterCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(ResolveBroadcasterCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadSingleOrDefault(new BroadcasterBySlugSpec(command.Slug), ct);

        if (broadcaster is null)
        {
            broadcaster = new Broadcaster(command.Slug, command.Name);
            broadcaster.Describe(command.Name, null, null, command.Kind);
            workspace.Add(broadcaster);
        }
        else
        {
            broadcaster.Describe(broadcaster.Name, broadcaster.ShortName, broadcaster.CallLetters, command.Kind);
        }

        var isCanonical = broadcaster.Slug.Equals(command.Slug, StringComparison.OrdinalIgnoreCase);
        var knownExternalId = broadcaster.ExternalIdFor(source.SourceKey);

        if (!isCanonical && knownExternalId is not null)
            return broadcaster;

        if (isCanonical && knownExternalId == command.ExternalId && !broadcaster.NeedsDetail)
            return broadcaster;

        broadcaster.TrackSource(source.SourceKey, command.ExternalId);
        await unitOfWork.SaveChanges(ct);

        var detail = await source.GetBroadcasterAsync(command.ExternalId, ct);
        if (detail is not null && isCanonical)
            broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, command.Kind);

        broadcaster.MarkDetailFetched();

        return broadcaster;
    }
}
