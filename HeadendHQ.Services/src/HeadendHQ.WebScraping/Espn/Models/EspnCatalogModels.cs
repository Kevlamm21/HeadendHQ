using System.Text.Json.Serialization;

namespace HeadendHQ.WebScraping.Espn.Models;

internal record EspnRefList(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("pageCount")] int PageCount,
    [property: JsonPropertyName("pageIndex")] int PageIndex,
    [property: JsonPropertyName("items")] List<EspnRef>? Items);

internal record EspnRef([property: JsonPropertyName("$ref")] string? Ref);

internal record EspnLogo(
    [property: JsonPropertyName("href")] string Href,
    [property: JsonPropertyName("rel")] List<string>? Rel,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("lastUpdated")] DateTimeOffset? LastUpdated);

internal record EspnSportDetail(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("displayName")] string? DisplayName);

internal record EspnLeagueDetail(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("abbreviation")] string? Abbreviation,
    [property: JsonPropertyName("shortName")] string? ShortName,
    [property: JsonPropertyName("isTournament")] bool? IsTournament,
    [property: JsonPropertyName("logos")] List<EspnLogo>? Logos);

internal record EspnTeamsResponse([property: JsonPropertyName("sports")] List<EspnTeamsSport>? Sports)
{
    public IEnumerable<EspnTeamDetail> Teams =>
        Sports?.SelectMany(s => s.Leagues ?? [])
              .SelectMany(l => l.Teams ?? [])
              .Select(t => t.Team)
              .Where(t => t is not null)
              .Select(t => t!)
        ?? [];
}

internal record EspnTeamsSport([property: JsonPropertyName("leagues")] List<EspnTeamsLeague>? Leagues);

internal record EspnTeamsLeague([property: JsonPropertyName("teams")] List<EspnTeamWrapper>? Teams);

internal record EspnTeamWrapper([property: JsonPropertyName("team")] EspnTeamDetail? Team);

internal record EspnTeamDetail(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("abbreviation")] string? Abbreviation,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("shortDisplayName")] string? ShortDisplayName,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("nickname")] string? Nickname,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("color")] string? Color,
    [property: JsonPropertyName("alternateColor")] string? AlternateColor,
    [property: JsonPropertyName("isActive")] bool? IsActive,
    [property: JsonPropertyName("logos")] List<EspnLogo>? Logos);

internal record EspnMediaDetail(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("shortName")] string? ShortName,
    [property: JsonPropertyName("callLetters")] string? CallLetters,
    [property: JsonPropertyName("logos")] List<EspnLogo>? Logos);
