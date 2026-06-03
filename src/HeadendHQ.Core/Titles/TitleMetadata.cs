namespace HeadendHQ.Core.Titles;

public record TitleMetadata
{
    public string? Plot { get; set; }
    public string? Tagline { get; set; }
    public int? Year { get; set; }
    public string? Studio { get; set; }
    public List<string> Genres { get; set; } = [];
    public List<string> Sets { get; set; } = [];
    public float? Rating { get; set; }
    public string? ContentRating { get; set; }
    public string? UniqueId { get; set; }
}
