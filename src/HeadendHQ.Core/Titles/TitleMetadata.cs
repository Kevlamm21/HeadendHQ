namespace HeadendHQ.Core.Titles;

/// <summary>
/// Jellyfin presentation fields, stored as JSON alongside the title.
/// <para>
/// Purely descriptive text now: the asset ids that used to live here moved to
/// <see cref="TitleArtwork"/> and <see cref="TitleCastMember"/>, where they are real columns with
/// real foreign keys rather than loose integers nothing could enforce.
/// </para>
/// </summary>
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
    public string? VenueName { get; set; }
}
