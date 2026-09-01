using HeadendHQ.Core.Catalog.Sources;
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

/// <summary>
/// What the image is for, which decides how it is normalized. Kept separate from the slot so the
/// catalog does not have to know about image processing.
/// </summary>
public enum ImagePurpose
{
    TeamLogo,
    LeagueLogo,
    BroadcasterLogo,
    Wordmark,
    Headshot
}

internal static class ImageStoreLock
{
    public static readonly SemaphoreSlim Gate = new(1, 1);
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

        var bytes = await Normalize(fetched.Bytes, command.Purpose, ct);
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
            var image = Image.Create(bytes, "image/png", width, height, ImageOrigin.Fetched);
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

    private Task<byte[]> Normalize(byte[] bytes, ImagePurpose purpose, CancellationToken ct) => purpose switch
    {
        ImagePurpose.TeamLogo => normalizer.NormalizeTeamLogoAsync(bytes, ct),
        ImagePurpose.LeagueLogo => normalizer.NormalizeLeagueLogoAsync(bytes, ct),
        ImagePurpose.BroadcasterLogo => normalizer.NormalizeStreamingLogoAsync(bytes, ct),
        ImagePurpose.Wordmark => normalizer.NormalizeWordMarkAsync(bytes, ct),
        ImagePurpose.Headshot => normalizer.NormalizeHeadshotAsync(bytes, ct),
        _ => Task.FromResult(bytes),
    };
}

/// <summary>Attaches hand-uploaded bytes to a slot. A manual image outranks anything fetched.</summary>
public record UploadImageCommand(ImageRef Slot, byte[] Bytes, ImagePurpose Purpose) : ICommand<int>;

public class UploadImageHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IImageNormalizer normalizer)
    : ICommandHandler<UploadImageCommand, int>
{
    public async ValueTask<int> Handle(UploadImageCommand command, CancellationToken ct)
    {
        var bytes = command.Purpose switch
        {
            ImagePurpose.TeamLogo => await normalizer.NormalizeTeamLogoAsync(command.Bytes, ct),
            ImagePurpose.LeagueLogo => await normalizer.NormalizeLeagueLogoAsync(command.Bytes, ct),
            ImagePurpose.BroadcasterLogo => await normalizer.NormalizeStreamingLogoAsync(command.Bytes, ct),
            ImagePurpose.Wordmark => await normalizer.NormalizeWordMarkAsync(command.Bytes, ct),
            ImagePurpose.Headshot => await normalizer.NormalizeHeadshotAsync(command.Bytes, ct),
            _ => command.Bytes,
        };

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
            var image = Image.Create(bytes, "image/png", width, height, ImageOrigin.Manual);
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
