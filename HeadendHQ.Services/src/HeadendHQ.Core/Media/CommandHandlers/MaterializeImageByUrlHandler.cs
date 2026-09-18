using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Media.CommandHandlers;

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

        if (!command.Revalidate)
        {
            var heldId = await readModel.SingleOrDefault(new ImageIdBySourceUrlSpec(url, command.Purpose), ct);

            if (heldId > 0)
                return heldId;
        }

        var held = await workspace.LoadSingleOrDefault(new ImageBySourceUrlSpec(url, command.Purpose), ct);

        FetchedImage? fetched;
        try
        {
            fetched = await fetcher.FetchAsync(url, held?.ETag, held?.LastModifiedUtc, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to download image from {Url}.", url);
            return held?.Id;
        }

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
            var current = await workspace.LoadSingleOrDefault(new ImageBySourceUrlSpec(url, command.Purpose), ct);

            if (current is not null)
            {
                if (current.Sha256 == hash)
                {
                    current.Revalidated(fetched.ETag, fetched.LastModifiedUtc);
                    return current.Id;
                }

                current.Supersede();
                await unitOfWork.SaveChanges(ct);
            }

            if (await workspace.LoadSingleOrDefault(new ImageByHashSpec(hash), ct) is { } existing)
            {
                if (existing.SourceUrl is null && existing.Purpose == command.Purpose)
                    existing.AdoptSource(url, fetched.ETag, fetched.LastModifiedUtc);

                return existing.Id;
            }

            var (width, height) = normalizer.Measure(bytes);
            var image = Image.Create(
                bytes, "image/png", width, height, ImageOrigin.Fetched, command.Purpose, url,
                command.LeagueId, fetched.ETag, fetched.LastModifiedUtc);

            workspace.Add(image);

            await unitOfWork.SaveChanges(ct);

            return image.Id;
        }
        finally
        {
            ImageStoreLock.Gate.Release();
        }
    }
}
