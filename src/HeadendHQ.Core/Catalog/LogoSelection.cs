using HeadendHQ.Core.Catalog.Sources;

namespace HeadendHQ.Core.Catalog;

public static class LogoSelection
{
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

    public static string LabelFor(ImageCandidate candidate) =>
        string.IsNullOrWhiteSpace(candidate.Rel) ? LogoRels.Default : candidate.Rel;

    private static IReadOnlyList<ImageCandidate> Materialize(IEnumerable<ImageCandidate>? candidates) =>
        candidates as IReadOnlyList<ImageCandidate> ?? [.. candidates ?? []];

    private static ImageCandidate? Pick(IEnumerable<ImageCandidate> candidates, string rel) =>
        candidates.FirstOrDefault(candidate =>
            LabelFor(candidate).Equals(rel, StringComparison.OrdinalIgnoreCase)
            && candidate.Url is { Length: > 0 });
}
