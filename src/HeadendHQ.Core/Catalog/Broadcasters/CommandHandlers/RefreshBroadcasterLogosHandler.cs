using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record RefreshBroadcasterLogosCommand(int BroadcasterId, bool RefreshExisting = false) : ICommand<Broadcaster>;

public class RefreshBroadcasterLogosHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IBroadcasterCatalogSource source,
    ILogger<RefreshBroadcasterLogosHandler> logger)
    : ICommandHandler<RefreshBroadcasterLogosCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(RefreshBroadcasterLogosCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        if (broadcaster.HasFetchedLogos && !command.RefreshExisting)
            return broadcaster;

        if (broadcaster.ExternalIdFor(source.SourceKey) is not { } externalId)
            return broadcaster;

        BroadcasterDescriptor? detail;
        try
        {
            detail = await source.GetBroadcasterAsync(externalId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read artwork for broadcaster {Slug}.", broadcaster.Slug);
            return broadcaster;
        }

        var (_, dropped) = await StoreLogosAsync(broadcaster, detail?.Logos, mediator, command.RefreshExisting, ct);

        await unitOfWork.SaveChanges(ct);
        await mediator.Send(new DeleteOrphanedImagesCommand(dropped), ct);

        return broadcaster;
    }

    internal static async Task<(LogoDownload Download, IReadOnlyList<int> Dropped)> StoreLogosAsync(
        Broadcaster broadcaster, IReadOnlyList<ImageCandidate>? candidates, IMediator mediator,
        bool revalidate, CancellationToken ct)
    {
        var download = await CatalogLogoDownloader.DownloadAsync(
            mediator, LogoPolicy.Broadcaster, candidates, ImagePurpose.BroadcasterLogo, revalidate, ct);

        return (download, broadcaster.StoreFetchedLogos(download));
    }

    internal static async Task<LogoDownload> FillLogosAsync(
        Broadcaster broadcaster, IReadOnlyList<ImageCandidate>? candidates, IMediator mediator, CancellationToken ct) =>
        broadcaster.HasFetchedLogos
            ? LogoDownload.Nothing
            : (await StoreLogosAsync(broadcaster, candidates, mediator, revalidate: false, ct)).Download;
}
