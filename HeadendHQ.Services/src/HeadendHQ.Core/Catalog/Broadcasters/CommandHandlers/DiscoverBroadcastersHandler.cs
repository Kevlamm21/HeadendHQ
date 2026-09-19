using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record DiscoverBroadcastersCommand(int? Max = null, bool Refresh = false) : ICommand<DiscoverBroadcastersResult>;

public record DiscoverBroadcastersResult(int Examined, int Created, int LogosAdded, bool SweepComplete);

public class DiscoverBroadcastersHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IBroadcasterCatalogSource source,
    ILogger<DiscoverBroadcastersHandler> logger)
    : ICommandHandler<DiscoverBroadcastersCommand, DiscoverBroadcastersResult>
{
    private const int CheckpointEvery = 50;

    public async ValueTask<DiscoverBroadcastersResult> Handle(DiscoverBroadcastersCommand command, CancellationToken ct)
    {
        var all = await workspace.LoadAll<Broadcaster>(ct);
        var bySlugOrAlias = new Dictionary<string, Broadcaster>(StringComparer.OrdinalIgnoreCase);
        var resolvedIds = new HashSet<string>();

        foreach (var b in all)
        {
            bySlugOrAlias.TryAdd(b.Slug, b);
            foreach (var alias in b.Aliases)
                bySlugOrAlias.TryAdd(alias, b);

            if (b.ExternalIdFor(source.SourceKey) is { } id)
                resolvedIds.Add(id);
        }

        var ids = await source.ListBroadcasterIdsAsync(ct);

        var examined = 0;
        var created = 0;
        var logosAdded = 0;
        var sinceCheckpoint = 0;
        var cappedOut = false;
        var droppedImages = new List<int>();

        try
        {
            foreach (var id in ids)
            {
                if (!command.Refresh && resolvedIds.Contains(id))
                    continue;

                if (command.Max is { } max && examined >= max)
                {
                    cappedOut = true;
                    break;
                }

                var detail = await source.GetBroadcasterAsync(id, ct);
                examined++;

                if (detail is null || string.IsNullOrEmpty(detail.Slug))
                    continue;

                var canonical = bySlugOrAlias.TryGetValue(detail.Slug, out var existing)
                    && existing!.Slug.Equals(detail.Slug, StringComparison.OrdinalIgnoreCase);

                if (existing is not null && !canonical)
                {
                    // An alias record only fills gaps; refreshing from it would replace the canonical record's logos.
                    logosAdded += await StoreLogosAsync(existing, detail.Logos, refresh: false, droppedImages, ct);
                }
                else
                {
                    var broadcaster = existing ?? new Broadcaster(detail.Slug, detail.Name);

                    broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, BroadcasterKind.Unknown);
                    broadcaster.TrackSource(source.SourceKey, detail.ExternalId);

                    logosAdded += await StoreLogosAsync(broadcaster, detail.Logos, command.Refresh, droppedImages, ct);

                    broadcaster.MarkDetailFetched();

                    if (existing is null)
                    {
                        workspace.Add(broadcaster);
                        bySlugOrAlias[detail.Slug] = broadcaster;
                        created++;
                    }

                    resolvedIds.Add(detail.ExternalId);
                }

                if (++sinceCheckpoint >= CheckpointEvery)
                {
                    await unitOfWork.SaveChanges(ct);
                    sinceCheckpoint = 0;
                }
            }
        }
        catch (CatalogSourceThrottledException ex)
        {
            await unitOfWork.SaveChanges(ct);
            await mediator.Send(new DeleteOrphanedImagesCommand(droppedImages), ct);
            logger.LogInformation(ex,
                "Broadcaster crawl stopped after {Examined} record(s); it resumes on the next run.", examined);
            return new DiscoverBroadcastersResult(examined, created, logosAdded, SweepComplete: false);
        }

        await unitOfWork.SaveChanges(ct);
        await mediator.Send(new DeleteOrphanedImagesCommand(droppedImages), ct);

        logger.LogInformation(
            "Broadcaster crawl: examined {Examined}, created {Created}, +{Logos} logo(s), refresh={Refresh}, complete={Complete}.",
            examined, created, logosAdded, command.Refresh, !cappedOut);

        return new DiscoverBroadcastersResult(examined, created, logosAdded, SweepComplete: !cappedOut);
    }

    private async Task<int> StoreLogosAsync(
        Broadcaster broadcaster, IReadOnlyList<LogoRequest>? candidates, bool refresh, List<int> dropped,
        CancellationToken ct)
    {
        if (!refresh && broadcaster.HasFetchedLogos)
            return 0;

        var download = await CatalogLogoDownloader.DownloadAsync(
            mediator, LogoPolicy.Broadcaster, candidates, ImagePurpose.BroadcasterLogo, refresh, ct);

        dropped.AddRange(broadcaster.StoreFetchedLogos(download));
        return download.Stored.Count;
    }
}
