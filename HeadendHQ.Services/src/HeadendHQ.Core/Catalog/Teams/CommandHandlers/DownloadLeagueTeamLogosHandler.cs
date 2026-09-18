using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
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
        var key = new LeagueKey(sport.Slug, league.Slug, league.ExternalIdFor(source.SourceKey));

        var byExternalId = new Dictionary<string, IReadOnlyList<ImageCandidate>>();
        foreach (var descriptor in await source.GetTeamsAsync(key, ct))
            if (descriptor.Logos is { Count: > 0 } logos)
                byExternalId.TryAdd(descriptor.ExternalId, logos);

        var candidates = new Dictionary<int, IReadOnlyList<ImageCandidate>>();
        foreach (var team in pending)
            if (team.ExternalIdFor(source.SourceKey) is { } externalId
                && byExternalId.TryGetValue(externalId, out var logos))
                candidates[team.Id] = logos;

        // ESPN's bulk NFL listing once handed every team the previous team's guid-addressed logos, so each team
        // was re-sourced from its own record first. It no longer reproduces; re-enable this (and the GuidAddressed
        // filter in EspnSportsCatalogSource.GetTeamsAsync) if logos start landing on the wrong team.
        // await VerifyLogosAsync(key, pending, candidates, ct);

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

    private async Task VerifyLogosAsync(
        LeagueKey key, IEnumerable<Team> teams,
        Dictionary<int, IReadOnlyList<ImageCandidate>> candidates, CancellationToken ct)
    {
        var budget = SourceSettings.MaxTeamLogoLookupsPerRun;
        var verified = 0;

        foreach (var team in teams)
        {
            if (verified >= budget)
                break;

            if (!team.LogosNeedVerifying)
                continue;

            if (team.ExternalIdFor(source.SourceKey) is not { } externalId)
                continue;

            IReadOnlyList<ImageCandidate> trusted;
            try
            {
                trusted = await source.GetTeamLogosAsync(new TeamKey(key, externalId), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to verify logos for team {Team}.", team.DisplayName);
                continue;
            }

            if (trusted.Count == 0)
                continue;

            candidates[team.Id] = trusted;
            team.MarkLogosVerified();
            verified++;
        }

        if (verified > 0)
        {
            await unitOfWork.SaveChanges(ct);
            logger.LogInformation(
                "Verified logo variants for {Count} team(s) in {League}{More}.",
                verified, key.LeagueSlug,
                verified >= budget ? "; more remain for the next run" : string.Empty);
        }
    }
}
