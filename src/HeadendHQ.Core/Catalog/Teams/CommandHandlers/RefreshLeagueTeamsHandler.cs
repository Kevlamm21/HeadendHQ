using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record RefreshLeagueTeamsCommand(int LeagueId, bool RefreshLogos = false) : ICommand<int>;

public class RefreshLeagueTeamsHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<RefreshLeagueTeamsHandler> logger)
    : ICommandHandler<RefreshLeagueTeamsCommand, int>
{
    public async ValueTask<int> Handle(RefreshLeagueTeamsCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        if (!league.SupportsTeams)
        {
            logger.LogInformation("League {League} has no teams to refresh.", league.Slug);
            return 0;
        }

        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);
        var key = new LeagueKey(sport.Slug, league.Slug, league.ExternalIdFor(source.SourceKey));

        var descriptors = await source.GetTeamsAsync(key, ct);
        var existing = (await workspace.Load(new TeamsByLeagueSpec(league.Id), ct))
            .ToDictionary(t => t.ExternalIdFor(source.SourceKey) ?? $"name:{t.DisplayName}");

        foreach (var descriptor in descriptors)
        {
            if (!existing.TryGetValue(descriptor.ExternalId, out var team)
                && !existing.TryGetValue($"name:{descriptor.DisplayName}", out team))
            {
                team = new Team(league.Id, descriptor.DisplayName, isFollowed: league.IsFollowed);
                workspace.Add(team);
                existing[descriptor.ExternalId] = team;
            }

            team.Describe(
                descriptor.DisplayName, descriptor.ShortDisplayName, descriptor.Slug, descriptor.Abbreviation,
                descriptor.Location, descriptor.Nickname, descriptor.PrimaryColorHex, descriptor.AlternateColorHex,
                descriptor.IsActive);
            team.TrackSource(source.SourceKey, descriptor.ExternalId);
        }

        league.MarkTeamsRefreshed();

        await unitOfWork.SaveChanges(ct);

        var candidates = new Dictionary<int, IReadOnlyList<ImageCandidate>>();

        foreach (var descriptor in descriptors)
            if (descriptor.Logos is { Count: > 0 } logos
                && (existing.TryGetValue(descriptor.ExternalId, out var team)
                    || existing.TryGetValue($"name:{descriptor.DisplayName}", out team)))
                candidates[team.Id] = logos;

        await VerifyLogosAsync(key, existing.Values, candidates, ct);
        await DownloadLogosAsync(existing.Values, candidates, command.RefreshLogos, ct);

        logger.LogInformation("Refreshed {Count} team(s) for {League}.", descriptors.Count, league.Slug);
        return descriptors.Count;
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

    private async Task DownloadLogosAsync(
        IEnumerable<Team> teams, Dictionary<int, IReadOnlyList<ImageCandidate>> candidates,
        bool refreshExisting, CancellationToken ct)
    {
        var downloaded = 0;

        foreach (var team in teams)
        {
            if (!candidates.TryGetValue(team.Id, out var options))
                continue;

            if (LogoSelection.ForTeam(options, team.PreferredLogoRel, !team.LogosNeedVerifying) is not { } chosen)
                continue;

            var label = LogoSelection.LabelFor(chosen);

            if (Logos.Find(team.Logos, LogoVariants.Default, label) is { } held
                && (!refreshExisting || held.Origin is ImageOrigin.Manual))
                continue;

            if (await mediator.Send(
                    new MaterializeImageByUrlCommand(chosen.Url, ImagePurpose.TeamLogo, Revalidate: refreshExisting), ct)
                is { } imageId)
            {
                team.UpsertLogo(label, imageId);
                downloaded++;
            }
        }

        if (downloaded > 0)
        {
            await unitOfWork.SaveChanges(ct);
            logger.LogInformation("Stored {Count} team logo(s).", downloaded);
        }
    }
}
