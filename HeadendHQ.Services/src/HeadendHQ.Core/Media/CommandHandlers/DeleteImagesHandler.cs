using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Media.CommandHandlers;

public record DeleteImagesCommand(IReadOnlyList<int> ImageIds) : ICommand<int>;

public class DeleteImagesHandler(IWorkspace workspace) : ICommandHandler<DeleteImagesCommand, int>
{
    public async ValueTask<int> Handle(DeleteImagesCommand command, CancellationToken ct)
    {
        var removed = 0;

        foreach (var id in command.ImageIds.Distinct())
        {
            if (await workspace.LoadSingleOrDefault(new ImageByIdSpec(id), ct) is not { } image)
                continue;

            if (image.Origin != ImageOrigin.Manual)
                continue;

            workspace.Remove(image);
            removed++;
        }

        return removed;
    }
}
