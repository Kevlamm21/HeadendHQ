using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record CleanupExpiredTitlesCommand(int RetentionDays) : ICommand<Unit>;

public class CleanupExpiredTitlesHandler(IWorkspace workspace, IUnitOfWork uow)
    : ICommandHandler<CleanupExpiredTitlesCommand, Unit>
{
    public async ValueTask<Unit> Handle(CleanupExpiredTitlesCommand command, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-command.RetentionDays);
        var expired = await workspace.Load(new ExpiredTitlesSpec(cutoff), ct);

        foreach (var title in expired)
            workspace.Remove(title);

        if (expired.Count > 0)
            await uow.SaveChanges(ct);

        return Unit.Value;
    }
}
