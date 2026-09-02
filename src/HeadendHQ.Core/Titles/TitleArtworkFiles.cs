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
}
