using HeadendHQ.Core.Catalog.Sources;

namespace HeadendHQ.Core.Catalog;

/// <summary>
/// Which of a source's candidate marks is worth downloading, per kind of catalog record.
/// <para>
/// This used to be a read-time fallback over stored rows, which only worked because every candidate
/// URL was recorded whether or not it was ever wanted — ESPN ships sixteen variants per team. Now
/// that a row means bytes on disk, the choice has to happen <em>before</em> the download, so the
/// chains live here and take candidates rather than rows.
/// </para>
/// </summary>
public static class LogoSelection
{
    /// <summary>The team mark worth downloading.</summary>
    /// <param name="verified">
    /// Whether these candidates came from the per-team lookup rather than a bulk listing. ESPN's bulk
    /// NFL listing hands every team the previous team's image guid, so until a team has been verified
    /// only the plain <see cref="LogoRels.Default"/> address can be trusted — downloading the
    /// preferred on-colour variant from an unverified listing would store another club's mark.
    /// </param>
    public static ImageCandidate? ForTeam(
        IEnumerable<ImageCandidate>? candidates, string preferredRel, bool verified)
    {
        var all = Materialize(candidates);

        if (!verified)
            return Pick(all, LogoRels.Default) ?? Pick(all, LogoRels.Scoreboard);

        return Pick(all, preferredRel)
            ?? Pick(all, LogoRels.OnSecondaryColor)
            ?? Pick(all, LogoRels.OnPrimaryColor)
            ?? Pick(all, LogoRels.Default)
            ?? Pick(all, LogoRels.Scoreboard)
            ?? all.FirstOrDefault();
    }

    /// <summary>
    /// The dark variant first — ESPN's networks publish a light-on-dark mark that reads on a
    /// team-coloured card — then the default, then whatever exists.
    /// </summary>
    public static ImageCandidate? ForBroadcaster(IEnumerable<ImageCandidate>? candidates)
    {
        var all = Materialize(candidates);

        return Pick(all, LogoRels.Dark)
            ?? Pick(all, LogoRels.Default)
            ?? all.FirstOrDefault();
    }

    public static ImageCandidate? ForLeague(IEnumerable<ImageCandidate>? candidates)
    {
        var all = Materialize(candidates);

        return Pick(all, LogoRels.Default)
            ?? Pick(all, LogoRels.Dark)
            ?? all.FirstOrDefault();
    }

    /// <summary>
    /// The label a candidate is stored under. A source may hand back tokens we have no constant for —
    /// the set is open — so this normalizes rather than restricts.
    /// </summary>
    public static string LabelFor(ImageCandidate candidate) =>
        string.IsNullOrWhiteSpace(candidate.Rel) ? LogoRels.Default : candidate.Rel;

    private static IReadOnlyList<ImageCandidate> Materialize(IEnumerable<ImageCandidate>? candidates) =>
        candidates as IReadOnlyList<ImageCandidate> ?? [.. candidates ?? []];

    private static ImageCandidate? Pick(IEnumerable<ImageCandidate> candidates, string rel) =>
        candidates.FirstOrDefault(candidate =>
            LabelFor(candidate).Equals(rel, StringComparison.OrdinalIgnoreCase)
            && candidate.Url is { Length: > 0 });
}
