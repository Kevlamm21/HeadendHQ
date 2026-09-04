using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

/// <summary>
/// Downloads a broadcaster's mark. Like a league's, this is on demand rather than at discovery: the
/// crawl walks ~1300 networks, most of them local affiliates with no artwork on file and no chance of
/// ever appearing on a poster.
/// </summary>
public record RefreshBroadcasterLogosCommand(int BroadcasterId, bool RefreshExisting = false) : ICommand<int>;

public class RefreshBroadcasterLogosHandler(
    IWorkspace workspace,
    IMediator mediator,
    IBroadcasterCatalogSource source,
    ILogger<RefreshBroadcasterLogosHandler> logger)
    : ICommandHandler<RefreshBroadcasterLogosCommand, int>
{
    public async ValueTask<int> Handle(RefreshBroadcasterLogosCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        if (broadcaster.PreferredLogo() is { } current
            && (!command.RefreshExisting || current.Origin is ImageOrigin.Manual))
            return 0;

        if (broadcaster.ExternalIdFor(source.SourceKey) is not { } externalId)
            return 0;

        BroadcasterDescriptor? detail;
        try
        {
            detail = await source.GetBroadcasterAsync(externalId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read artwork for broadcaster {Slug}.", broadcaster.Slug);
            return 0;
        }

        return await StoreLogoAsync(broadcaster, detail?.Logos, mediator, command.RefreshExisting, ct);
    }

    /// <summary>
    /// Shared with the handlers that already hold a broadcaster's record, so a mark is downloaded
    /// from detail in hand rather than by asking for it a second time.
    /// </summary>
    internal static async Task<int> StoreLogoAsync(
        Broadcaster broadcaster, IReadOnlyList<ImageCandidate>? candidates, IMediator mediator,
        bool refreshExisting, CancellationToken ct)
    {
        if (LogoSelection.ForBroadcaster(candidates) is not { } chosen)
            return 0;

        var label = LogoSelection.LabelFor(chosen);

        if (Logos.Find(broadcaster.Logos, LogoVariants.Default, label) is { } held
            && (!refreshExisting || held.Origin is ImageOrigin.Manual))
            return 0;

        if (await mediator.Send(
                new MaterializeImageByUrlCommand(
                    chosen.Url, ImagePurpose.BroadcasterLogo, Revalidate: refreshExisting), ct)
            is not { } imageId)
            return 0;

        broadcaster.UpsertLogo(label, imageId);
        return 1;
    }
}
