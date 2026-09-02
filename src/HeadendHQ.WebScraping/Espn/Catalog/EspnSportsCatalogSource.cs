using System.Text.Json;
using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.WebScraping.Espn.Models;
using HeadendHQ.WebScraping.Espn.Transport;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Catalog;

/// <summary>
/// Reads ESPN's reference data and translates it into source-neutral descriptors.
/// <para>
/// The one non-obvious decision here is reading slugs out of <c>$ref</c> URLs instead of following
/// them. ESPN's collection endpoints return nothing but links, so a naive walk of the 356 leagues
/// would cost 356 requests before we learned a single slug — but the slug is already the last
/// path segment of every link.
/// </para>
/// </summary>
internal sealed class EspnSportsCatalogSource(
    EspnTransport transport,
    ILogger<EspnSportsCatalogSource> logger) : ISportsCatalogSource
{
    /// <summary>Sports with no meaningful team roster; their teams endpoint is not worth calling.</summary>
    private static readonly HashSet<string> IndividualSports =
        new(StringComparer.OrdinalIgnoreCase) { "golf", "tennis", "mma", "racing" };

    public string SourceKey => SourceKeys.Espn;

    public async Task<IReadOnlyList<SportDescriptor>> GetSportsAsync(CancellationToken ct)
    {
        var slugs = await GetRefSlugsAsync(EspnEndpoints.Sports(), ct);
        var sports = new List<SportDescriptor>();

        foreach (var slug in slugs)
        {
            var detail = await TryGetAsync<EspnSportDetail>(EspnEndpoints.Sport(slug), ct);
            sports.Add(new SportDescriptor(
                detail?.Id ?? slug,
                slug,
                Coalesce(detail?.DisplayName, detail?.Name) ?? Humanize(slug)));
        }

        return sports;
    }

    public async Task<IReadOnlyList<LeagueDescriptor>> GetLeaguesAsync(string sportSlug, CancellationToken ct)
    {
        var slugs = await GetRefSlugsAsync(EspnEndpoints.Leagues(sportSlug), ct);
        var leagues = new List<LeagueDescriptor>();

        foreach (var slug in slugs)
        {
            var detail = await TryGetAsync<EspnLeagueDetail>(
                EspnEndpoints.League(sportSlug, slug), ct);

            leagues.Add(new LeagueDescriptor(
                ExternalId: detail?.Id ?? slug,
                Slug: slug,
                Name: Coalesce(detail?.DisplayName, detail?.Name) ?? Humanize(slug),
                Abbreviation: detail?.Abbreviation,
                ShortName: detail?.ShortName,
                SupportsTeams: !IndividualSports.Contains(sportSlug),
                Logos: ToCandidates(detail?.Logos)));
        }

        return leagues;
    }

    public async Task<IReadOnlyList<TeamDescriptor>> GetTeamsAsync(LeagueKey league, CancellationToken ct)
    {
        // One request returns the whole league with colours and all sixteen logo variants.
        var json = await transport.GetStringAsync(
            EspnEndpoints.Teams(league.SportSlug, league.LeagueSlug), ct);

        var response = JsonSerializer.Deserialize<EspnTeamsResponse>(json);

        return [.. response!.Teams
            .Where(t => t.Id is not null)
            .Select(t => new TeamDescriptor(
                ExternalId: t.Id!,
                DisplayName: t.DisplayName ?? t.Name ?? t.Slug ?? t.Id!,
                ShortDisplayName: t.ShortDisplayName,
                Slug: t.Slug,
                Abbreviation: t.Abbreviation,
                Location: t.Location,
                Nickname: t.Nickname,
                PrimaryColorHex: t.Color,
                AlternateColorHex: t.AlternateColor,
                IsActive: t.IsActive ?? true,
                // Deliberately not the guid variants: see GetTeamLogosAsync.
                Logos: ToCandidates(t.Logos, GuidAddressed, keep: false)))];
    }

    /// <summary>
    /// The guid-addressed logo variants, fetched one team at a time from the core API.
    /// <para>
    /// The bulk teams listing carries these too, and for the NFL every single one of them is wrong:
    /// ESPN hands each team the guid belonging to the team with the next-lower id, so Green Bay
    /// (id 9) gets Detroit's (id 8) mark. The core API's per-team record returns the correct guid.
    /// Guids are stable, so a caller only ever needs to do this once per team.
    /// </para>
    /// </summary>
    public async Task<IReadOnlyList<ImageCandidate>> GetTeamLogosAsync(TeamKey team, CancellationToken ct)
    {
        var detail = await TryGetAsync<EspnTeamDetail>(
            EspnEndpoints.Team(team.League.SportSlug, team.League.LeagueSlug, team.TeamExternalId), ct);

        return ToCandidates(detail?.Logos, GuidAddressed, keep: true) ?? [];
    }

    public async Task<IReadOnlyList<AthleteDescriptor>> GetRosterAsync(TeamKey team, CancellationToken ct)
    {
        try
        {
            var json = await transport.GetStringAsync(
                EspnEndpoints.Roster(team.League.SportSlug, team.League.LeagueSlug, team.TeamExternalId), ct);

            return [.. EspnRoster.FlattenActiveAthletes(json)
                .Where(a => !string.IsNullOrEmpty(a.Id))
                .Select(a => new AthleteDescriptor(
                    ExternalId: a.Id,
                    DisplayName: a.DisplayName ?? "Unknown",
                    ShortName: a.ShortName,
                    Position: a.Position?.DisplayName ?? a.Position?.Abbreviation,
                    Jersey: a.Jersey,
                    ExperienceYears: a.Experience?.Years,
                    HeadshotUrl: a.Headshot?.Href,
                    TeamExternalId: team.TeamExternalId))];
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not EspnThrottledException)
        {
            logger.LogWarning(ex, "Failed to fetch {League} roster for team {TeamId}.",
                team.League.LeagueSlug, team.TeamExternalId);
            return [];
        }
    }

    public async Task<IReadOnlyDictionary<string, int>> GetDepthChartAsync(
        TeamKey team, int seasonYear, CancellationToken ct) =>
        await EspnDepthChart.FetchAsync(
            transport, team.League.SportSlug, team.League.LeagueSlug, team.TeamExternalId, seasonYear, logger, ct);

    /// <summary>
    /// Pulls the last path segment out of each <c>$ref</c>. ESPN's collections are link-only, but the
    /// links are addressed by slug, so this replaces one request per item with zero.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetRefSlugsAsync(string url, CancellationToken ct)
    {
        var json = await transport.GetStringAsync(url, ct);
        var list = JsonSerializer.Deserialize<EspnRefList>(json);

        return [.. (list?.Items ?? [])
            .Select(i => i.Ref)
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => EspnRefs.LastSegment(r!))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private async Task<T?> TryGetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(await transport.GetStringAsync(url, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not EspnThrottledException)
        {
            // A single unreadable league must not abandon the whole discovery run.
            logger.LogWarning(ex, "Failed to read {Url}; continuing.", url);
            return default;
        }
    }

    /// <summary>An <c>a.espncdn.com/guid/{guid}/logos/...</c> asset, as opposed to an abbreviation-keyed one.</summary>
    private static bool GuidAddressed(EspnLogo logo) =>
        logo.Href.Contains("/guid/", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<ImageCandidate>? ToCandidates(
        List<EspnLogo>? logos, Func<EspnLogo, bool>? predicate = null, bool keep = true) =>
        logos is null ? null
        : [.. logos
            .Where(l => !string.IsNullOrEmpty(l.Href))
            .Where(l => predicate is null || predicate(l) == keep)
            .Select(l => new ImageCandidate(
                LogoRels.Normalize(l.Rel ?? []), l.Href, l.Width, l.Height, l.LastUpdated))];

    private static string? Coalesce(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) ? a : !string.IsNullOrWhiteSpace(b) ? b : null;

    /// <summary>Fallback display name when ESPN gives us nothing but the slug.</summary>
    private static string Humanize(string slug) =>
        string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length <= 3 ? part.ToUpperInvariant() : char.ToUpperInvariant(part[0]) + part[1..]));
}
