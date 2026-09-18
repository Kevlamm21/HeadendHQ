using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Media.CommandHandlers;

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
                bytes, ContentTypeFor(command.Purpose), width, height, ImageOrigin.Manual, command.Purpose);
            workspace.Add(image);
            await unitOfWork.SaveChanges(ct);

            return image.Id;
        }
        finally
        {
            ImageStoreLock.Gate.Release();
        }
    }

    private static string ContentTypeFor(ImagePurpose purpose) => purpose switch
    {
        ImagePurpose.Poster or ImagePurpose.Background or ImagePurpose.Thumbnail => "image/jpeg",
        _ => "image/png",
    };
}
