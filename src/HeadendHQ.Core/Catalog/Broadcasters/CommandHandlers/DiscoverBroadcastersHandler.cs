using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Iptv;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

/// <summary>
/// Crawls ESPN's media index for the full broadcaster catalogue — every network, with both logo
/// variants — so the settings list is complete before a game has aired on each one.
/// <para>
/// The index is two requests; each record is one more, ~1311 in total, which fits one run's request
/// budget. An interrupted crawl resumes by skipping ids already on a row, so re-running it after a
/// truncated pass picks up where it stopped rather than stranding every network past the cut-off.
/// It runs automatically once, when the database is first created; after that it is manual
/// (<c>POST /catalog/broadcasters/discover</c>).
/// </para>
/// </summary>
public record DiscoverBroadcastersCommand(int? Max = null) : ICommand<DiscoverBroadcastersResult>;

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
        var lineup = LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));

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

        try
        {
            foreach (var id in ids)
            {
                if (resolvedIds.Contains(id))
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
                    // Alias sighting: artwork only. It must not rename the row or claim its id, or
                    // the canonical record would never be read.
                    logosAdded += await RefreshBroadcasterLogosHandler.StoreLogoAsync(
                        existing, detail.Logos, mediator, refreshExisting: false, ct);
                }
                else
                {
                    var broadcaster = existing ?? new Broadcaster(detail.Slug, detail.Name);

                    broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, BroadcasterKind.Unknown);
                    broadcaster.TrackSource(source.SourceKey, detail.ExternalId);

                    // The crawl walks ~1300 networks, most of them local affiliates with nothing on
                    // file. Artwork is downloaded only where it can actually be used, so the crawl
                    // stays a catalogue pass rather than a bulk image import.
                    if (broadcaster.IsSubscribed)
                        logosAdded += await RefreshBroadcasterLogosHandler.StoreLogoAsync(
                            broadcaster, detail.Logos, mediator, refreshExisting: false, ct);

                    broadcaster.MarkDetailFetched();

                    if (existing is null)
                    {
                        BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
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
            logger.LogInformation(ex,
                "Broadcaster crawl stopped after {Examined} record(s); it resumes on the next run.", examined);
            return new DiscoverBroadcastersResult(examined, created, logosAdded, SweepComplete: false);
        }

        await unitOfWork.SaveChanges(ct);

        logger.LogInformation(
            "Broadcaster crawl: examined {Examined}, created {Created}, +{Logos} logo(s), complete={Complete}.",
            examined, created, logosAdded, !cappedOut);

        return new DiscoverBroadcastersResult(examined, created, logosAdded, SweepComplete: !cappedOut);
    }
}
