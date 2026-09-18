using HeadendHQ.Core.Catalog.Sources;

namespace HeadendHQ.Core.Catalog;

public sealed class LogoPolicy
{
    public static readonly LogoPolicy Team = new(
        [LogoRels.OnPrimaryColor, LogoRels.OnSecondaryColor, LogoRels.SecondaryOnPrimaryColor],
        [LogoRels.Default],
        LogoRels.OnPrimaryColor);

    public static readonly LogoPolicy Broadcaster = new(
        [LogoRels.Default, LogoRels.Dark],
        [],
        LogoRels.Dark);

    public static readonly LogoPolicy League = new(
        [LogoRels.Default],
        [LogoRels.Dark],
        LogoRels.Default);

    private LogoPolicy(string[] pull, string[] fallback, string selected)
    {
        Pull = pull;
        Fallback = fallback;
        Selected = selected;
    }

    public IReadOnlyList<string> Pull { get; }

    public IReadOnlyList<string> Fallback { get; }

    public string Selected { get; }

    public IReadOnlyList<ImageCandidate> Choose(IEnumerable<ImageCandidate>? candidates)
    {
        var usable = (candidates ?? []).Where(c => c.Url is { Length: > 0 }).ToList();

        var chosen = Pick(usable, Pull);
        return chosen.Count > 0 ? chosen : Pick(usable, Fallback);
    }

    public string? DefaultLabel(IEnumerable<string?> heldLabels)
    {
        var held = heldLabels.OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new[] { Selected }.Concat(Pull).Concat(Fallback).FirstOrDefault(held.Contains);
    }

    public static string LabelFor(ImageCandidate candidate) =>
        string.IsNullOrWhiteSpace(candidate.Rel) ? LogoRels.Default : candidate.Rel;

    private static List<ImageCandidate> Pick(List<ImageCandidate> candidates, IEnumerable<string> rels) =>
        [.. rels
            .Select(rel => candidates.FirstOrDefault(c => LabelFor(c).Equals(rel, StringComparison.OrdinalIgnoreCase)))
            .OfType<ImageCandidate>()];
}
