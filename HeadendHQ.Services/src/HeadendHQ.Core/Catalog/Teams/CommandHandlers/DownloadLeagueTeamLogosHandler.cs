using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record DownloadLeagueTeamLogosCommand(int LeagueId, bool RefreshExisting = false) : ICommand<int>;

public class DownloadLeagueTeamLogosHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<DownloadLeagueTeamLogosHandler> logger)
    : ICommandHandler<DownloadLeagueTeamLogosCommand, int>
{
    public async ValueTask<int> Handle(DownloadLeagueTeamLogosCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        if (!league.SupportsTeams)
            return 0;

        var pending = (await workspace.Load(new TeamsByLeagueSpec(league.Id), ct))
            .Where(team => command.RefreshExisting || !team.HasFetchedLogos)
            .ToList();

        if (pending.Count == 0)
            return 0;

        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);
        var key = new LeagueKey(sport.Slug, league.Slug);

        var byExternalId = new Dictionary<string, IReadOnlyList<LogoRequest>>();
        foreach (var descriptor in await source.GetTeamsAsync(key, ct))
            if (descriptor.Logos is { Count: > 0 } logos)
                byExternalId.TryAdd(descriptor.ExternalId, logos);

        var candidates = new Dictionary<int, IReadOnlyList<LogoRequest>>();
        foreach (var team in pending)
            if (team.ExternalIdFor(source.SourceKey) is { } externalId
                && byExternalId.TryGetValue(externalId, out var logos))
                candidates[team.Id] = logos;

        var stored = 0;
        var dropped = new List<int>();

        foreach (var team in pending)
        {
            if (!candidates.TryGetValue(team.Id, out var options))
                continue;

            var download = await CatalogLogoDownloader.DownloadAsync(
                mediator, LogoPolicy.Team, options, ImagePurpose.TeamLogo, command.RefreshExisting, ct);

            dropped.AddRange(team.StoreFetchedLogos(download));

            if (download.Stored.Count > 0)
                stored++;
        }

        await unitOfWork.SaveChanges(ct);
        await mediator.Send(new DeleteOrphanedImagesCommand(dropped), ct);

        logger.LogInformation(
            "Stored logos for {Stored} of {Pending} team(s) in {League}.", stored, pending.Count, league.Slug);

        return stored;
    }
}
