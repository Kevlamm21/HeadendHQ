using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record DeleteAllLeagueTeamImagesCommand(int LeagueId) : ICommand<int>;

public class DeleteAllLeagueTeamImagesHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IMediator mediator)
    : ICommandHandler<DeleteAllLeagueTeamImagesCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllLeagueTeamImagesCommand command, CancellationToken ct)
    {
        var teams = await workspace.Load(new TeamsByLeagueSpec(command.LeagueId), ct);
        var candidates = teams.SelectMany(team => team.Logos).Select(logo => logo.ImageId).ToHashSet();

        foreach (var team in teams)
            team.ClearLogos();

        await unitOfWork.SaveChanges(ct);

        return await mediator.Send(new DeleteOrphanedImagesCommand(candidates), ct);
    }
}
