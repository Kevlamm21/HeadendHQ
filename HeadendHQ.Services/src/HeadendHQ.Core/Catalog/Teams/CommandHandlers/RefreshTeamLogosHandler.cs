using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record RefreshTeamLogosCommand(int TeamId, bool RefreshExisting = false) : ICommand<Team>;

public class RefreshTeamLogosHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<RefreshTeamLogosHandler> logger)
    : ICommandHandler<RefreshTeamLogosCommand, Team>
{
    public async ValueTask<Team> Handle(RefreshTeamLogosCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        if (team.HasFetchedLogos && !command.RefreshExisting)
            return team;

        if (team.ExternalIdFor(source.SourceKey) is not { } externalId)
            return team;

        var league = await workspace.LoadById<League, int>(team.LeagueId, ct);
        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);
        var key = new LeagueKey(sport.Slug, league.Slug, league.ExternalIdFor(source.SourceKey));

        IReadOnlyList<ImageCandidate> candidates;
        try
        {
            candidates = await source.GetTeamLogosAsync(new TeamKey(key, externalId), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read artwork for team {Team}.", team.DisplayName);
            return team;
        }

        if (candidates.Count == 0)
            return team;

        team.MarkLogosVerified();

        var download = await CatalogLogoDownloader.DownloadAsync(
            mediator, LogoPolicy.Team, candidates, ImagePurpose.TeamLogo, command.RefreshExisting, ct);

        var dropped = team.StoreFetchedLogos(download);

        await unitOfWork.SaveChanges(ct);
        await mediator.Send(new DeleteOrphanedImagesCommand(dropped), ct);

        return team;
    }
}
