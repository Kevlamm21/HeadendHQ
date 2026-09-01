namespace HeadendHQ.Espn.Transport;

/// <summary>
/// ESPN's collection endpoints return nothing but <c>$ref</c> links, but the last path segment of
/// each link is the thing itself — a slug for sports and leagues, a numeric id for media. Reading it
/// straight from the URL replaces one request per item with zero.
/// </summary>
internal static class EspnRefs
{
    public static string LastSegment(string href)
    {
        var path = href.Split('?', 2)[0].TrimEnd('/');
        var lastSlash = path.LastIndexOf('/');
        return lastSlash < 0 ? string.Empty : path[(lastSlash + 1)..];
    }
}
