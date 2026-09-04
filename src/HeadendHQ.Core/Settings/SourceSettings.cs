namespace HeadendHQ.Core.Settings;

/// <summary>
/// How hard we are willing to lean on an upstream source.
/// <para>
/// These are settings rather than constants because the right pace depends on how much of a
/// catalog you follow: a first-run discovery across every league wants a gentler cadence than a
/// nightly seven-day scrape of two leagues. Defaults are chosen to look like a person browsing.
/// </para>
/// </summary>
public class SourceSettings
{
    public int Id { get; private set; }

    /// <summary>Ceiling on sustained request rate, enforced as a token bucket.</summary>
    public int RequestsPerMinute { get; private set; } = 60;

    /// <summary>Floor on the gap between two requests.</summary>
    public int MinDelayMs { get; private set; } = 400;

    /// <summary>Random extra delay on top of <see cref="MinDelayMs"/>, so the cadence is not a metronome.</summary>
    public int JitterMs { get; private set; } = 400;

    public int MaxConcurrency { get; private set; } = 2;

    /// <summary>Hard stop for a single operation, so a bug cannot turn into thousands of requests.</summary>
    public int PerRunRequestBudget { get; private set; } = 1500;

    public string UserAgent { get; private set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36";

    /// <summary>
    /// Which sports the initial catalog discovery walks leagues for. Every sport is still recorded so
    /// the UI can offer it, but only these have their (often hundreds of) leagues enumerated. Anything
    /// else is pulled in on demand.
    /// </summary>
    public List<string> DiscoverySportSlugs { get; private set; } = ["football", "basketball", "baseball"];

    /// <summary>
    /// Ceiling on per-team logo lookups in one league refresh. Guids are confirmed once per team and
    /// then cached forever, so a large league simply finishes over several nightly runs instead of
    /// firing 760 requests in one go.
    /// </summary>
    public int MaxTeamLogoLookupsPerRun { get; private set; } = 64;

    public void Configure(
        int? requestsPerMinute, int? minDelayMs, int? jitterMs, int? maxConcurrency,
        int? perRunRequestBudget, string? userAgent,
        IEnumerable<string>? discoverySportSlugs = null, int? maxTeamLogoLookupsPerRun = null)
    {
        if (requestsPerMinute is > 0) RequestsPerMinute = requestsPerMinute.Value;
        if (minDelayMs is >= 0) MinDelayMs = minDelayMs.Value;
        if (jitterMs is >= 0) JitterMs = jitterMs.Value;
        if (maxConcurrency is > 0) MaxConcurrency = maxConcurrency.Value;
        if (perRunRequestBudget is > 0) PerRunRequestBudget = perRunRequestBudget.Value;
        if (!string.IsNullOrWhiteSpace(userAgent)) UserAgent = userAgent;
        if (maxTeamLogoLookupsPerRun is >= 0) MaxTeamLogoLookupsPerRun = maxTeamLogoLookupsPerRun.Value;

        if (discoverySportSlugs is not null)
            DiscoverySportSlugs = [.. discoverySportSlugs
                .Where(slug => !string.IsNullOrWhiteSpace(slug))
                .Select(slug => slug.Trim().ToLowerInvariant())
                .Distinct()];
    }

    public bool CoversSport(string sportSlug) =>
        DiscoverySportSlugs.Count == 0
        || DiscoverySportSlugs.Contains(sportSlug, StringComparer.OrdinalIgnoreCase);
}
