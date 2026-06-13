namespace HeadendHQ.Core.Titles;

public record TitleMetadata
{
    public string? Plot { get; set; }
    public string? Tagline { get; set; }
    public int? Year { get; set; }
    public string? Studio { get; set; }
    public List<string> Genres { get; set; } = [];
    public List<string> Sets { get; set; } = [];
    public string? Rating { get; set; }
    public string? ContentRating { get; set; }
    public string? UniqueId { get; set; }

    public int? HomeTeamAssetId { get; set; }
    public int? AwayTeamAssetId { get; set; }
    public int? LeagueAssetId { get; set; }
    public int? StreamingServiceAssetId { get; set; }
    public int? WordMarkId { get; set; }
}
