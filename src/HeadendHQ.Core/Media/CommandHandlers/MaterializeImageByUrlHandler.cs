using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Media.CommandHandlers;

/// <summary>
/// Downloads what lives at an upstream URL and returns the id of the row holding it. The single way
/// bytes enter the store from a source.
/// <para>
/// The URL check comes first and is the whole point: content addressing dedupes only <em>after</em>
/// the bytes are in hand, so a cast rebuilt on every game would re-download the same face every
/// time. A player we have seen before costs zero requests.
/// </para>
/// </summary>
/// <param name="Revalidate">
/// Whether to re-check an address we already hold. Off for anything whose bytes never change at a
/// stable address — a player's headshot is the same photo until media day, and re-asking nightly for
/// every billed athlete would undo the saving above. On for logos, where a rebrand does land at the
/// old address and the nightly refresh is what is meant to notice; an unchanged mark then costs a
/// conditional request with no body.
/// </param>
public record MaterializeImageByUrlCommand(
    string SourceUrl, ImagePurpose Purpose, int? LeagueId = null, bool Revalidate = false) : ICommand<int?>;

public class MaterializeImageByUrlHandler(
    IWorkspace workspace,
    IReadModel readModel,
    IUnitOfWork unitOfWork,
    IImageFetcher fetcher,
    IImageNormalizer normalizer,
    ILogger<MaterializeImageByUrlHandler> logger)
    : ICommandHandler<MaterializeImageByUrlCommand, int?>
{
    public async ValueTask<int?> Handle(MaterializeImageByUrlCommand command, CancellationToken ct)
    {
        if (command.SourceUrl is not { Length: > 0 } url)
            return null;

        // Projected to the id: this is the hot path, and dragging the BLOB back for every billed
        // player only to read a key would undo the saving. The projection is a value type, so a
        // miss comes back as 0 rather than null — ids start at 1.
        if (!command.Revalidate)
        {
            var heldId = await readModel.SingleOrDefault(new ImageIdBySourceUrlSpec(url, command.Purpose), ct);

            if (heldId > 0)
                return heldId;
        }

        // Read once for its validators. The authoritative re-read happens under the gate below,
        // because the download in between is where another caller can settle the same address.
        var held = await workspace.LoadSingleOrDefault(new ImageBySourceUrlSpec(url, command.Purpose), ct);

        FetchedImage? fetched;
        try
        {
            // Conditional headers claim "I already hold these bytes", so they may only be sent when
            // that is true — otherwise a 304 would be an answer we cannot satisfy.
            fetched = await fetcher.FetchAsync(url, held?.ETag, held?.LastModifiedUtc, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A missing logo must never fail the operation that wanted it; artwork falls back.
            logger.LogWarning(ex, "Failed to download image from {Url}.", url);
            return held?.Id;
        }

        // 304: what we already hold is current.
        if (fetched is null)
        {
            held?.Revalidated();
            return held?.Id;
        }

        var bytes = await normalizer.NormalizeAsync(fetched.Bytes, command.Purpose, ct);
        var hash = Image.ComputeHash(bytes);

        await ImageStoreLock.Gate.WaitAsync(ct);
        try
        {
            // Re-read rather than trusting the row loaded before the download: two callers can miss
            // the fast path together, and whichever gets here second must see what the first stored
            // or its insert would collide on the unique (SourceUrl, Purpose) index.
            var current = await workspace.LoadSingleOrDefault(new ImageBySourceUrlSpec(url, command.Purpose), ct);

            if (current is not null)
            {
                // The bytes at this address are what we just downloaded — either because nothing
                // changed, or because the caller ahead of us stored the same thing. No new row and no
                // repointing, just fresh validators so the next refresh can ask conditionally again.
                if (current.Sha256 == hash)
                {
                    current.Revalidated(fetched.ETag, fetched.LastModifiedUtc);
                    return current.Id;
                }

                // The address now holds different bytes. The old row keeps its own — what is served
                // at an image id may never change, and Jellyfin caches it as immutable — but gives up
                // its claim on the address so the replacement can take it.
                current.Supersede();
                await unitOfWork.SaveChanges(ct);
            }

            if (await workspace.LoadSingleOrDefault(new ImageByHashSpec(hash), ct) is { } existing)
            {
                // Two addresses converged on identical bytes. Letting the row we already have take
                // the address over is what stops this from re-downloading on every refresh. Only
                // when it is the same purpose: the address is half of a key the other half of which
                // is how the bytes were normalized.
                if (existing.SourceUrl is null && existing.Purpose == command.Purpose)
                    existing.AdoptSource(url, fetched.ETag, fetched.LastModifiedUtc);

                return existing.Id;
            }

            var (width, height) = normalizer.Measure(bytes);
            var image = Image.Create(
                bytes, "image/png", width, height, ImageOrigin.Fetched, command.Purpose, url,
                command.LeagueId, fetched.ETag, fetched.LastModifiedUtc);

            workspace.Add(image);

            // The identity is assigned by the insert, and callers store a plain id rather than a
            // navigation, so it has to be flushed before there is anything to point at.
            await unitOfWork.SaveChanges(ct);

            return image.Id;
        }
        finally
        {
            ImageStoreLock.Gate.Release();
        }
    }
}
