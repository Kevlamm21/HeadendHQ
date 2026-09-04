using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Media.CommandHandlers;

/// <summary>
/// Stores hand-uploaded bytes and returns the id. The caller attaches it — a manual image outranks
/// anything fetched, and it is the owning collection that records which is which.
/// </summary>
public record UploadImageCommand(byte[] Bytes, ImagePurpose Purpose) : ICommand<int>;

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
            if (await workspace.LoadSingleOrDefault(new ImageByHashSpec(hash), ct) is { } existing)
                return existing.Id;

            var (width, height) = normalizer.Measure(bytes);
            var image = Image.Create(
                bytes, "image/png", width, height, ImageOrigin.Manual, command.Purpose);
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
