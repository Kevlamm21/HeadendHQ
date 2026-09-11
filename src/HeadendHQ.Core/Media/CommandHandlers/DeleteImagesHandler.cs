using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Media.CommandHandlers;

/// <summary>
/// Removes specific image rows by id. Used when the thing that owned them goes away — a produced
/// title is cleaned up, or one of its artwork slots is re-uploaded and the old bytes are now
/// unreferenced. Ids that no longer exist are skipped.
/// </summary>
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

            // Only ever remove title-owned rows. A shared fetched logo that somehow lands in a slot
            // is left alone — its catalog owner is the one that deletes it.
            if (image.Origin != ImageOrigin.Manual)
                continue;

            workspace.Remove(image);
            removed++;
        }

        return removed;
    }
}
