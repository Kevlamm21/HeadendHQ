using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog;

/// <summary>
/// Single-row marker recording whether first-run discovery has happened.
/// <para>
/// Discovery is expensive and must run exactly once per database, never again on a restart or a
/// redeploy against an existing volume. The completed timestamp is written only after every league
/// has been walked; until then <see cref="CompletedLeagueSlugs"/> lets an interrupted run resume
/// instead of starting over.
/// </para>
/// </summary>
public class CatalogSyncState : IEntity<int>
{
    public int Id { get; init; }

    public DateTimeOffset? InitialDiscoveryStartedAtUtc { get; private set; }
    public DateTimeOffset? InitialDiscoveryCompletedAtUtc { get; private set; }
    public DateTimeOffset? LastBroadcasterRefreshUtc { get; private set; }

    public string Stage { get; private set; } = "NotStarted";
    public string? LastError { get; private set; }

    public List<string> CompletedLeagueSlugs { get; private set; } = [];

    public bool NeedsInitialDiscovery => InitialDiscoveryCompletedAtUtc is null;

    public void Begin()
    {
        InitialDiscoveryStartedAtUtc ??= DateTimeOffset.UtcNow;
        LastError = null;
        Stage = "Running";
    }

    public void EnterStage(string stage) => Stage = stage;

    public void MarkLeagueCompleted(string slug)
    {
        if (!CompletedLeagueSlugs.Contains(slug))
            CompletedLeagueSlugs.Add(slug);
    }

    public bool IsLeagueCompleted(string slug) => CompletedLeagueSlugs.Contains(slug);

    public void Complete()
    {
        InitialDiscoveryCompletedAtUtc = DateTimeOffset.UtcNow;
        Stage = "Completed";
        LastError = null;
    }

    public void Fail(string error)
    {
        LastError = error;
        Stage = "Failed";
    }

    public void MarkBroadcastersRefreshed() => LastBroadcasterRefreshUtc = DateTimeOffset.UtcNow;

    /// <summary>Forces the next run to walk everything again, keeping the data already stored.</summary>
    public void Reset()
    {
        InitialDiscoveryCompletedAtUtc = null;
        InitialDiscoveryStartedAtUtc = null;
        CompletedLeagueSlugs.Clear();
        Stage = "NotStarted";
        LastError = null;
    }
}
