using System.Text.Json;
using HeadendHQ.Core;
using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.WebScraping.Espn.Models;
using HeadendHQ.WebScraping.Espn.Transport;
using HeadendHQ.WebScraping.Transport;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Catalog;

internal sealed class EspnCatalogSource(
    WebTransport transport,
    ILogger<EspnCatalogSource> logger) : ISportsCatalogSource, IBroadcasterCatalogSource
{
    private static readonly HashSet<string> IndividualSports =
        new(StringComparer.OrdinalIgnoreCase) { "golf", "tennis", "mma", "racing" };

    public string SourceKey => SourceKeys.Espn;

    public async Task<IReadOnlyList<SportRequest>> GetSportsAsync(CancellationToken ct)
    {
        var slugs = await GetRefSlugsAsync(EspnEndpoints.Sports(), ct);
        var sports = new List<SportRequest>();

        foreach (var slug in slugs)
        {
            var detail = await TryGetAsync<EspnSportDetail>(EspnEndpoints.Sport(slug), ct);
            sports.Add(new SportRequest(
                detail?.Id ?? slug,
                slug,
                Coalesce(detail?.DisplayName, detail?.Name) ?? Humanize(slug)));
        }

        return sports;
    }

    public async Task<IReadOnlyList<LeagueRequest>> GetLeaguesAsync(string sportSlug, CancellationToken ct)
    {
        var slugs = await GetRefSlugsAsync(EspnEndpoints.Leagues(sportSlug), ct);
        var leagues = new List<LeagueRequest>();

        foreach (var slug in slugs)
        {
            var detail = await TryGetAsync<EspnLeagueDetail>(
                EspnEndpoints.League(sportSlug, slug), ct);

            leagues.Add(ToLeague(sportSlug, slug, detail));
        }

        return leagues;
    }

    public async Task<LeagueRequest?> GetLeagueAsync(string sportSlug, string leagueSlug, CancellationToken ct)
    {
        var detail = await TryGetAsync<EspnLeagueDetail>(EspnEndpoints.League(sportSlug, leagueSlug), ct);

        return detail is null ? null : ToLeague(sportSlug, leagueSlug, detail);
    }

    private static LeagueRequest ToLeague(string sportSlug, string slug, EspnLeagueDetail? detail) =>
        new(
            ExternalId: detail?.Id ?? slug,
            Slug: slug,
            Name: Coalesce(detail?.DisplayName, detail?.Name) ?? Humanize(slug),
            Abbreviation: detail?.Abbreviation,
            ShortName: detail?.ShortName,
            SupportsTeams: !IndividualSports.Contains(sportSlug),
            Logos: ToCandidates(detail?.Logos));

    public async Task<IReadOnlyList<TeamRequest>> GetTeamsAsync(LeagueKey league, CancellationToken ct)
    {
        var json = await transport.GetStringAsync(
            EspnEndpoints.Teams(league.SportSlug, league.LeagueSlug), ct);

        var response = JsonSerializer.Deserialize<EspnTeamsResponse>(json);

        return [.. response!.Teams
            .Where(t => t.Id is not null)
            .Select(t => new TeamRequest(
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
                Logos: ToCandidates(t.Logos)))];
    }

    public async Task<IReadOnlyList<LogoRequest>> GetTeamLogosAsync(TeamKey team, CancellationToken ct)
    {
        var detail = await TryGetAsync<EspnTeamDetail>(
            EspnEndpoints.Team(team.League.SportSlug, team.League.LeagueSlug, team.TeamExternalId), ct);

        return ToCandidates(detail?.Logos, GuidAddressed, keep: true) ?? [];
    }

    public async Task<IReadOnlyList<string>> ListBroadcasterIdsAsync(CancellationToken ct)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>();

        for (var page = 1; ; page++)
        {
            ct.ThrowIfCancellationRequested();

            var json = await transport.GetStringAsync(EspnEndpoints.MediaIndex(page), ct);
            var list = JsonSerializer.Deserialize<EspnRefList>(json);

            foreach (var item in list?.Items ?? [])
            {
                if (string.IsNullOrEmpty(item.Ref))
                    continue;

                var id = EspnRefs.LastSegment(item.Ref);
                if (id.Length > 0 && id.All(char.IsDigit) && seen.Add(id))
                    ids.Add(id);
            }

            if (list is null || list.Items is null or { Count: 0 } || page >= list.PageCount)
                break;
        }

        logger.LogInformation("ESPN media index: {Count} broadcaster id(s).", ids.Count);
        return ids;
    }

    public async Task<BroadcasterRequest?> GetBroadcasterAsync(string externalId, CancellationToken ct)
    {
        if (await TryGetAsync<EspnMediaDetail>(EspnEndpoints.Media(externalId), ct) is not { Slug: not null } media)
            return null;

        return new BroadcasterRequest(
            ExternalId: media.Id ?? externalId,
            Slug: media.Slug,
            Name: media.Name ?? media.Slug,
            ShortName: media.ShortName,
            CallLetters: media.CallLetters,
            Logos: ToCandidates(media.Logos));
    }

    private async Task<IReadOnlyList<string>> GetRefSlugsAsync(string url, CancellationToken ct)
    {
        var slugs = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var page = 1; ; page++)
        {
            ct.ThrowIfCancellationRequested();

            var paged = page == 1 ? url : $"{url}&page={page}";
            var list = JsonSerializer.Deserialize<EspnRefList>(await transport.GetStringAsync(paged, ct));

            foreach (var slug in (list?.Items ?? [])
                         .Select(i => i.Ref)
                         .Where(r => !string.IsNullOrEmpty(r))
                         .Select(r => EspnRefs.LastSegment(r!))
                         .Where(s => s.Length > 0))
                if (seen.Add(slug))
                    slugs.Add(slug);

            if (list is null || list.Items is null or { Count: 0 } || page >= list.PageCount)
                break;
        }

        return slugs;
    }

    private async Task<T?> TryGetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(await transport.GetStringAsync(url, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not CatalogSourceThrottledException)
        {
            logger.LogWarning(ex, "Failed to read {Url}; continuing.", url);
            return default;
        }
    }

    private static bool GuidAddressed(EspnLogo logo) =>
        logo.Href.Contains("/guid/", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<LogoRequest>? ToCandidates(
        List<EspnLogo>? logos, Func<EspnLogo, bool>? predicate = null, bool keep = true) =>
        logos is null ? null
        : [.. logos
            .Where(l => !string.IsNullOrEmpty(l.Href))
            .Where(l => predicate is null || predicate(l) == keep)
            .Select(l => new LogoRequest(LogoRels.Normalize(l.Rel ?? []), l.Href))];

    private static string? Coalesce(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) ? a : !string.IsNullOrWhiteSpace(b) ? b : null;

    private static string Humanize(string slug) =>
        string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length <= 3 ? part.ToUpperInvariant() : char.ToUpperInvariant(part[0]) + part[1..]));
}
