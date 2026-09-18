namespace HeadendHQ.WebScraping.Espn.Transport;

internal static class EspnRefs
{
    public static string LastSegment(string href)
    {
        var path = href.Split('?', 2)[0].TrimEnd('/');
        var lastSlash = path.LastIndexOf('/');
        return lastSlash < 0 ? string.Empty : path[(lastSlash + 1)..];
    }
}
