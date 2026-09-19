namespace HeadendHQ.Core.Settings;

public static class SourceSettings
{
    // Rate limits and headers now belong to each site's transport profile; only the shared default
    // user agent is common to all of them.
    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36";
}
