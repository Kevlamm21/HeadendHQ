namespace HeadendHQ.Core.Settings;

/// <summary>
/// How hard we are willing to lean on an upstream source.
/// <para>
/// Constants rather than stored settings: nothing here is a per-install preference, and holding
/// them in a table meant a value edited in code never reached a database that had already been
/// seeded. Tuning the pace is a deploy, and every install then agrees. Chosen to look like a
/// person browsing.
/// </para>
/// </summary>
public static class SourceSettings
{
    /// <summary>Ceiling on sustained request rate, enforced as a token bucket.</summary>
    public const int RequestsPerMinute = 60;

    /// <summary>Floor on the gap between two requests.</summary>
    public const int MinDelayMs = 400;

    /// <summary>Random extra delay on top of <see cref="MinDelayMs"/>, so the cadence is not a metronome.</summary>
    public const int JitterMs = 400;

    public const int MaxConcurrency = 2;

    /// <summary>Hard stop for a single operation, so a bug cannot turn into thousands of requests.</summary>
    public const int PerRunRequestBudget = 1500;

    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36";

    /// <summary>
    /// Ceiling on per-team logo lookups in one league refresh. Guids are confirmed once per team and
    /// then cached forever, so a large league simply finishes over several nightly runs instead of
    /// firing 760 requests in one go.
    /// </summary>
    public const int MaxTeamLogoLookupsPerRun = 64;

    /// <summary>
    /// Which sports the initial catalog discovery walks leagues for. Every sport is still recorded so
    /// the UI can offer it, but only these have their (often hundreds of) leagues enumerated. Anything
    /// else is pulled in on demand, so this only decides what the first run does unattended.
    /// </summary>
    public static readonly string[] DiscoverySportSlugs = ["football", "basketball", "baseball"];

    public static bool CoversSport(string sportSlug) =>
        DiscoverySportSlugs.Contains(sportSlug, StringComparer.OrdinalIgnoreCase);
}
