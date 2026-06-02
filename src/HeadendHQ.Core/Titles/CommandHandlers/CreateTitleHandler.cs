using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record CreateTitleCommand(TitleRequest Request) : ICommand<Title>;

public class CreateTitleHandler(IWorkspace workspace, IUnitOfWork uow)
    : ICommandHandler<CreateTitleCommand, Title>
{
    public async ValueTask<Title> Handle(CreateTitleCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var existing = await FindExistingAsync(request, ct);

        if (existing is not null)
        {
            existing.Update(request);
            await uow.SaveChanges(ct);
            return existing;
        }

        var title = new Title(request);
        workspace.Add(title);
        await uow.SaveChanges(ct);
        return title;
    }

    private async Task<Title?> FindExistingAsync(TitleRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.ExternalId))
        {
            var results = await workspace.Load(
                new TitleByProviderExternalIdSpec(request.Provider, request.ExternalId), ct);
            return results.FirstOrDefault();
        }

        var byName = await workspace.Load(
            new TitleByProviderNameStartSpec(request.Provider, request.Name, request.StartUtc), ct);
        return byName.FirstOrDefault();
    }
}
