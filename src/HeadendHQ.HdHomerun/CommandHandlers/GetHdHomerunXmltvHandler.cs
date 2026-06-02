using HeadendHQ.Core;
using Mediator;

namespace HeadendHQ.HdHomerun.CommandHandlers;

public record GetHdHomerunXmltvQuery : ICommand<string?>;

public class GetHdHomerunXmltvHandler(IHdHomerunService service)
    : ICommandHandler<GetHdHomerunXmltvQuery, string?>
{
    public async ValueTask<string?> Handle(GetHdHomerunXmltvQuery query, CancellationToken ct)
    {
        return await service.GetXmltvContentAsync(ct);
    }
}
