namespace HeadendHQ.Core.Titles;

public record TitleMetadata
{
    public string? Plot { get; set; }
    public string? Tagline { get; set; }
    public int? Year { get; set; }
    public string? Studio { get; set; }
    public string? Genre { get; set; }
    public float? Rating { get; set; }
    public string? ContentRating { get; set; }
    public string? UniqueId { get; set; }

    // --- Episode/Event specific ---
    public int? Season { get; set; }
    public int? Episode { get; set; }
    public string? ShowTitle { get; set; }

    // --- Sporting event specific ---
    public string? HomeTeam { get; set; }
    public string? AwayTeam { get; set; }
    public string? Venue { get; set; }
    public string? League { get; set; }

}