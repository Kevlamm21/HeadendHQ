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

/// <summary>
/// Pulls a league's teams — one request returns every team with colours and all logo variants.
/// Run when a league is followed, and again on the nightly refresh for followed leagues.
/// </summary>
/// <param name="RefreshLogos">
/// Re-check the marks of teams that already have one. Off by default: a logo row means bytes we
/// already hold, so an ordinary refresh only downloads for teams that have nothing, and a large
/// league costs its downloads once rather than every night.
/// </param>
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
            // The name fallback is what carries the catalog across a source swap: rows still
            // stamped with the old source answer to no id the new one knows, and inserting a
            // second team with the same name would violate (LeagueId, DisplayName). TrackSource
            // below re-stamps the row, so the miss happens once per team and then never again.
            if (!existing.TryGetValue(descriptor.ExternalId, out var team)
                && !existing.TryGetValue($"name:{descriptor.DisplayName}", out team))
            {
                team = new Team(league.Id, descriptor.DisplayName);
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

        // A team inserted above has no identity until this flushes, which is why the candidate map
        // below is built afterwards rather than as the loop goes.
        await unitOfWork.SaveChanges(ct);

        // Candidates are held here rather than written to the team, because a logo row now means
        // bytes and the bulk listing is not yet trustworthy enough to download from — see
        // VerifyLogosAsync, which replaces these for the teams it gets to this run.
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

    /// <summary>
    /// Re-sources the logo variants the bulk listing cannot be trusted for, one team at a time.
    /// <para>
    /// ESPN's bulk NFL listing hands every team the previous team id's image guid, so all the
    /// on-colour marks come back as the wrong club. Correcting it costs one request per team, but
    /// image addresses are stable, so it happens once per team and then never again. The per-run cap
    /// is what keeps a 759-team league from turning a nightly refresh into a stampede: the remainder
    /// is simply picked up by the next run.
    /// </para>
    /// </summary>
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
                // One unreadable team must not abandon the rest; it stays unverified and is retried.
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

    /// <summary>
    /// Turns the chosen candidate into bytes. An unverified team gets only the plain default mark —
    /// the one address a bulk listing is always right about — and picks up its preferred on-colour
    /// variant on the run that verifies it.
    /// </summary>
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

            // A hand upload is never displaced by a refresh, so there is nothing to spend a request
            // on — the check is here as well as in the upsert so the download is skipped outright.
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
