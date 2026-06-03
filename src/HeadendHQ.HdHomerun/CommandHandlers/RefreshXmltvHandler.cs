using HeadendHQ.Core;
using Mediator;

namespace HeadendHQ.HdHomerun.CommandHandlers;

public record RefreshXmltvCommand : ICommand<Unit>;

public class RefreshXmltvHandler(IHdHomerunService service)
    : ICommandHandler<RefreshXmltvCommand, Unit>
{
    public async ValueTask<Unit> Handle(RefreshXmltvCommand command, CancellationToken ct)
    {
        await service.RefreshXmltvAsync(ct);
        return Unit.Value;
    }
}
