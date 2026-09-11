namespace HeadendHQ.Core.Media;

/// <summary>
/// What an image is for.
/// <para>
/// Does double duty: it decides how the bytes are normalized on the way in, and it is recorded on
/// the row so a whole class of image can be swept in one query — which is what makes a season-start
/// "throw away last year's headshots" job a single delete rather than a walk of the catalog.
/// </para>
/// </summary>
public enum ImagePurpose
{
    TeamLogo,
    LeagueLogo,
    BroadcasterLogo,
    Wordmark,
    Headshot,
    Poster,
    Background,
    Thumbnail
}
