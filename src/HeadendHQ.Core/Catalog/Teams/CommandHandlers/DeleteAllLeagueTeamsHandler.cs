using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

/// <summary>Debugging aid: removes a league's teams so they can be re-pulled with fresh logos.</summary>
public record DeleteAllLeagueTeamsCommand(int LeagueId) : ICommand<int>;

public class DeleteAllLeagueTeamsHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllLeagueTeamsCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllLeagueTeamsCommand command, CancellationToken ct)
    {
        var teams = await workspace.Load(new TeamsByLeagueSpec(command.LeagueId), ct);

        foreach (var team in teams)
            workspace.Remove(team);

        return teams.Count;
    }
}
