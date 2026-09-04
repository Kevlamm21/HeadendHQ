using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Media.CommandHandlers;

/// <summary>
/// Downloads whatever an <see cref="ImageRef"/> points at and attaches the bytes, unless the source
/// says nothing has changed.
/// </summary>
public record MaterializeImageCommand(ImageRef Slot, ImagePurpose Purpose) : ICommand<int?>;

internal static class ImageStoreLock
{
    public static readonly SemaphoreSlim Gate = new(1, 1);
}

internal static class ImageNormalization
{
    /// <summary>Normalization is chosen by purpose, so every entry point resolves it the same way.</summary>
    public static Task<byte[]> NormalizeAsync(
        this IImageNormalizer normalizer, byte[] bytes, ImagePurpose purpose, CancellationToken ct) =>
        purpose switch
        {
            ImagePurpose.TeamLogo => normalizer.NormalizeTeamLogoAsync(bytes, ct),
            ImagePurpose.LeagueLogo => normalizer.NormalizeLeagueLogoAsync(bytes, ct),
            ImagePurpose.BroadcasterLogo => normalizer.NormalizeStreamingLogoAsync(bytes, ct),
            ImagePurpose.Wordmark => normalizer.NormalizeWordMarkAsync(bytes, ct),
            ImagePurpose.Headshot => normalizer.NormalizeHeadshotAsync(bytes, ct),
            _ => Task.FromResult(bytes),
        };
}

public class MaterializeImageHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IImageFetcher fetcher,
    IImageNormalizer normalizer,
    ILogger<MaterializeImageHandler> logger)
    : ICommandHandler<MaterializeImageCommand, int?>
{
    public async ValueTask<int?> Handle(MaterializeImageCommand command, CancellationToken ct)
    {
        var slot = command.Slot;

        if (slot.SourceUrl is not { Length: > 0 } url)
            return slot.ImageId;

        // Conditional headers claim "I already hold these bytes", so they may only be sent when that
        // is true. A slot with validators but no image — a failed materialize, or an image row that
        // was removed — must re-download rather than accept a 304 it cannot satisfy.
        var validated = slot.IsMaterialized &&
            await workspace.LoadSingleOrDefault(new ImageByIdSpec(slot.ImageId!.Value), ct) is not null;

        FetchedImage? fetched;
        try
        {
            fetched = await fetcher.FetchAsync(
                url, validated ? slot.ETag : null, validated ? slot.LastModifiedUtc : null, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A missing logo must never fail the operation that wanted it; artwork falls back.
            logger.LogWarning(ex, "Failed to download image from {Url}.", url);
            return slot.ImageId;
        }

        // 304: what we already hold is current.
        if (fetched is null)
        {
            slot.MarkUnchanged();
            return slot.ImageId;
        }

        var bytes = await normalizer.NormalizeAsync(fetched.Bytes, command.Purpose, ct);
        var hash = Image.ComputeHash(bytes);

        // Content addressing does double duty: it dedupes identical logos across teams, and it means
        // a re-download that produced the same bytes costs no new row and no repointing.
        await ImageStoreLock.Gate.WaitAsync(ct);
        try
        {
            var existing = await workspace.LoadSingleOrDefault(new ImageByHashSpec(hash), ct);
            if (existing is not null)
            {
                slot.Materialize(existing.Id, fetched.ETag, fetched.LastModifiedUtc);
                return existing.Id;
            }

            var (width, height) = normalizer.Measure(bytes);
            var image = Image.Create(
                bytes, "image/png", width, height, ImageOrigin.Fetched, command.Purpose, url);
            workspace.Add(image);

            // The identity is assigned by the insert, and the slot stores a plain id rather than a
            // navigation, so it has to be flushed before there is anything to point at.
            await unitOfWork.SaveChanges(ct);

            slot.Materialize(image.Id, fetched.ETag, fetched.LastModifiedUtc);
            return image.Id;
        }
        finally
        {
            ImageStoreLock.Gate.Release();
        }
    }
}

/// <summary>
/// Downloads an image identified only by its upstream URL, with no slot to hang validators on.
/// <para>
/// The URL check comes first and is the whole point: content addressing dedupes only <em>after</em>
/// the bytes are in hand, so a cast rebuilt on every game would re-download the same face every
/// time. A player we have seen before costs zero requests.
/// </para>
/// </summary>
public record MaterializeImageByUrlCommand(
    string SourceUrl, ImagePurpose Purpose, int? LeagueId = null) : ICommand<int?>;

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
        var url = command.SourceUrl;

        // Projected to the id: this is the hot path, and dragging the BLOB back for every billed
        // player only to read a key would undo the saving. The projection is a value type, so a
        // miss comes back as 0 rather than null — ids start at 1.
        var heldId = await readModel.SingleOrDefault(new ImageIdBySourceUrlSpec(url), ct);

        if (heldId > 0)
            return heldId;

        FetchedImage? fetched;
        try
        {
            // Nothing is held for this URL, so there are no validators to send and a 304 would be
            // a lie we could not satisfy.
            fetched = await fetcher.FetchAsync(url, etag: null, lastModifiedUtc: null, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A missing face must never fail the detail collection that wanted it.
            logger.LogWarning(ex, "Failed to download image from {Url}.", url);
            return null;
        }

        if (fetched is null)
            return null;

        var bytes = await normalizer.NormalizeAsync(fetched.Bytes, command.Purpose, ct);
        var hash = Image.ComputeHash(bytes);

        await ImageStoreLock.Gate.WaitAsync(ct);
        try
        {
            // Re-checked under the gate: two events billing the same player can both miss above, and
            // if the source served them different bytes the hash check alone would not stop the
            // second insert from colliding on the unique SourceUrl index.
            if (await workspace.LoadSingleOrDefault(new ImageBySourceUrlSpec(url), ct) is { } raced)
                return raced.Id;

            if (await workspace.LoadSingleOrDefault(new ImageByHashSpec(hash), ct) is { } existing)
                return existing.Id;

            var (width, height) = normalizer.Measure(bytes);
            var image = Image.Create(
                bytes, "image/png", width, height, ImageOrigin.Fetched, command.Purpose, url, command.LeagueId);

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

/// <summary>Attaches hand-uploaded bytes to a slot. A manual image outranks anything fetched.</summary>
public record UploadImageCommand(ImageRef Slot, byte[] Bytes, ImagePurpose Purpose) : ICommand<int>;

public class UploadImageHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IImageNormalizer normalizer)
    : ICommandHandler<UploadImageCommand, int>
{
    public async ValueTask<int> Handle(UploadImageCommand command, CancellationToken ct)
    {
        var bytes = await normalizer.NormalizeAsync(command.Bytes, command.Purpose, ct);

        var hash = Image.ComputeHash(bytes);
        await ImageStoreLock.Gate.WaitAsync(ct);
        try
        {
            var existing = await workspace.LoadSingleOrDefault(new ImageByHashSpec(hash), ct);
            if (existing is not null)
            {
                command.Slot.AttachUpload(existing.Id);
                return existing.Id;
            }

            var (width, height) = normalizer.Measure(bytes);
            var image = Image.Create(
                bytes, "image/png", width, height, ImageOrigin.Manual, command.Purpose);
            workspace.Add(image);
            await unitOfWork.SaveChanges(ct);

            command.Slot.AttachUpload(image.Id);
            return image.Id;
        }
        finally
        {
            ImageStoreLock.Gate.Release();
        }
    }
}

public record GetImageQuery(int Id) : IQuery<Image?>;

public class GetImageHandler(IReadModel readModel) : IQueryHandler<GetImageQuery, Image?>
{
    public async ValueTask<Image?> Handle(GetImageQuery query, CancellationToken ct) =>
        await readModel.SingleOrDefault(new ImageByIdSpec(query.Id), ct);
}

/// <summary>
/// Throws away every image of one kind so it is fetched fresh.
/// <para>
/// Exists for media day: leagues reshoot player headshots each preseason at the same URLs, and the
/// URL check that makes a face cost one request ever is exactly what stops the new photo from ever
/// being noticed. Deleting the rows is how you tell the app to look again.
/// </para>
/// <para>
/// Cast rows pointing at a deleted image are left to rot into a 404. Only games within the scrape
/// window exist at all, and they are cleared once played, so the blast radius is the handful of
/// events already collected today — all of which re-collect below and are correct by tomorrow.
/// </para>
/// </summary>
public record ClearImagesByPurposeCommand(ImagePurpose Purpose, int? LeagueId = null) : ICommand<int>;

public class ClearImagesByPurposeHandler(
    IWorkspace workspace,
    ILogger<ClearImagesByPurposeHandler> logger)
    : ICommandHandler<ClearImagesByPurposeCommand, int>
{
    public async ValueTask<int> Handle(ClearImagesByPurposeCommand command, CancellationToken ct)
    {
        var images = await workspace.Load(new ImagesByPurposeSpec(command.Purpose, command.LeagueId), ct);

        if (images.Count == 0)
            return 0;

        foreach (var image in images)
            workspace.Remove(image);

        // Detail is collected once per event, ever, so a game already scraped would keep its now
        // headshot-less cast forever. Clearing the stamp is what makes "wipe it and let the next
        // scrape refill it" actually true.
        if (command.Purpose == ImagePurpose.Headshot)
            foreach (var sportingEvent in await workspace.Load(
                         new FutureEventsForRecollectSpec(DateTime.UtcNow, command.LeagueId), ct))
                sportingEvent.ResetDetails();

        logger.LogInformation(
            "Cleared {Count} {Purpose} image(s){Scope}.",
            images.Count, command.Purpose,
            command.LeagueId is { } leagueId ? $" for league {leagueId}" : string.Empty);

        return images.Count;
    }
}
