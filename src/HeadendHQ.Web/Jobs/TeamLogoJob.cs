using HeadendHQ.Core.Catalog.Teams.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Jobs;

public class TeamLogoJob(IMediator mediator)
{
    public async Task RunAsync(int leagueId, bool refreshExisting, CancellationToken ct) =>
        await mediator.Send(new DownloadLeagueTeamLogosCommand(leagueId, refreshExisting), ct);
}
