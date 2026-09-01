namespace HeadendHQ.Core.Titles;

/// <summary>
/// The file names a produced title's folder uses, in one place so the writer and the reader cannot
/// drift apart.
/// <para>
/// These are not arbitrary. Jellyfin's local image scanner recognises images by suffix, and it treats
/// <c>fanart</c>, <c>backdrop</c>, <c>background</c> and <c>art</c> all as backdrops — which is why
/// naming the two landscape renders <c>-fanart</c> and <c>-background</c> made both of them
/// backdrops and left the library with no thumbnail at all. <c>-thumb</c> is the name it reads as a
/// landscape thumbnail.
/// </para>
/// </summary>
public static class TitleArtworkFiles
{
    /// <summary>Portrait cover. Jellyfin: Primary.</summary>
    public static string Poster(string titleName) => $"{titleName}.jpg";

    /// <summary>Landscape backdrop. Jellyfin: Backdrop.</summary>
    public static string Backdrop(string titleName) => $"{titleName}-fanart.jpg";

    /// <summary>Landscape thumbnail. Jellyfin: Thumb.</summary>
    public static string Thumb(string titleName) => $"{titleName}-thumb.jpg";

    /// <summary>Transparent wordmark. Jellyfin: Logo.</summary>
    public static string ClearLogo(string titleName) => $"{titleName}-clearlogo.png";

    /// <summary>
    /// Where cast headshots are written. The leading dot keeps Jellyfin's media scanner out of it —
    /// these are referenced explicitly from the NFO and must not be mistaken for extra content.
    /// </summary>
    public const string ActorFolder = ".actors";

    /// <summary>
    /// A headshot's path relative to the title folder, which is what the NFO records. Built from the
    /// billing order as well as the name so two people who sanitize to the same string cannot
    /// collide.
    /// </summary>
    public static string ActorThumb(TitleCastMember member) =>
        $"{ActorFolder}/{member.Order:D2}-{Sanitize(member.Name)}.jpg";

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string([.. name.Select(c => invalid.Contains(c) ? '_' : c)]).Trim();
        return cleaned.Length == 0 ? "actor" : cleaned;
    }
}
