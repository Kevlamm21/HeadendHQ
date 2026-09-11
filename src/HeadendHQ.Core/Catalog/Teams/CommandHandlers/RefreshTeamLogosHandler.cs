using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record RefreshTeamLogosCommand(int TeamId, bool RefreshExisting = false) : ICommand<int>;

public class RefreshTeamLogosHandler(
    IWorkspace workspace,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<RefreshTeamLogosHandler> logger)
    : ICommandHandler<RefreshTeamLogosCommand, int>
{
    public async ValueTask<int> Handle(RefreshTeamLogosCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        if (team.PreferredLogo() is { } current
            && current.Label == team.PreferredLogoRel
            && (!command.RefreshExisting || current.Origin is ImageOrigin.Manual))
            return 0;

        if (team.ExternalIdFor(source.SourceKey) is not { } externalId)
            return 0;

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
            return 0;
        }

        if (candidates.Count == 0)
            return 0;

        team.MarkLogosVerified();

        if (LogoSelection.ForTeam(candidates, team.PreferredLogoRel, verified: true) is not { } chosen)
            return 0;

        var label = LogoSelection.LabelFor(chosen);

        if (Logos.Find(team.Logos, LogoVariants.Default, label) is { } held
            && (!command.RefreshExisting || held.Origin is ImageOrigin.Manual))
            return 0;

        if (await mediator.Send(
                new MaterializeImageByUrlCommand(
                    chosen.Url, ImagePurpose.TeamLogo, Revalidate: command.RefreshExisting), ct)
            is not { } imageId)
            return 0;

        team.UpsertLogo(label, imageId);
        return 1;
    }
}
