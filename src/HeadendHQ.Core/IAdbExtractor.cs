namespace HeadendHQ.Core;

/// <summary>
/// Turns a watch URL into an Android intent for one service.
/// <para>
/// Keyed by broadcaster slug rather than an enum, so the set of launchable services is data. ESPN
/// alone spans espn, espn2, espnu, espnplus and espn-unlimited; those are aliases of one broadcaster,
/// and the canonical slug is what a title carries.
/// </para>
/// </summary>
public interface IAdbExtractor
{
    string BroadcasterSlug { get; }
    Task<string?> BuildCommandAsync(string? eventUrl, CancellationToken ct);
}
