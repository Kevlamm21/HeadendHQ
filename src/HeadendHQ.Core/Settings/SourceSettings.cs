namespace HeadendHQ.Core.Settings;

public static class SourceSettings
{
    public const int RequestsPerMinute = 60;

    public const int MinDelayMs = 400;

    public const int JitterMs = 400;

    public const int MaxConcurrency = 2;

    public const int PerRunRequestBudget = 1500;

    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36";

    public const int CdnRequestsPerMinute = 300;

    public const int CdnMinDelayMs = 100;

    public const int CdnJitterMs = 100;

    public const int CdnMaxConcurrency = 4;

    public const int MaxTeamLogoLookupsPerRun = 64;

    public static readonly string[] DiscoverySportSlugs = ["football", "basketball", "baseball", "hockey"];

    public static bool CoversSport(string sportSlug) =>
        DiscoverySportSlugs.Contains(sportSlug, StringComparer.OrdinalIgnoreCase);
}
