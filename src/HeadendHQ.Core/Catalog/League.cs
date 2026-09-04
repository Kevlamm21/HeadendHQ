using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog;

public class League : IEntity<int>, IExternalRef
{
    private League() { }

    public League(int sportId, string slug, string name)
    {
        SportId = sportId;
        Slug = slug;
        Name = name;
    }

    public int Id { get; init; }
    public int SportId { get; private set; }

    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Abbreviation { get; private set; }
    public string? ShortName { get; private set; }

    /// <summary>Whether the user wants events from this league.</summary>
    public bool IsFollowed { get; private set; }

    /// <summary>
    /// False for individual sports (golf, tennis, MMA, racing) where a team roster is meaningless
    /// and the teams endpoint has nothing useful to say.
    /// </summary>
    public bool SupportsTeams { get; private set; } = true;

    public DateTimeOffset? TeamsRefreshedAtUtc { get; private set; }

    public string? SourceKey { get; private set; }
    public string? ExternalId { get; private set; }

    public List<LeagueLogo> Logos { get; private set; } = [];
    public List<LeagueWordmark> Wordmarks { get; private set; } = [];

    public void Describe(string name, string? abbreviation, string? shortName, bool supportsTeams)
    {
        Name = name;
        Abbreviation = abbreviation;
        ShortName = shortName;
        SupportsTeams = supportsTeams;
    }

    public void Follow(bool followed) => IsFollowed = followed;

    public void MarkTeamsRefreshed() => TeamsRefreshedAtUtc = DateTimeOffset.UtcNow;

    public void TrackSource(string sourceKey, string externalId)
    {
        SourceKey = sourceKey;
        ExternalId = externalId;
    }

    /// <summary>
    /// Picks the logo for an event variant, falling back to the league default. Lets an NBA Cup game
    /// use the Cup mark while an ordinary game uses the league mark, without either being required.
    /// </summary>
    public LeagueLogo? LogoFor(string variant) =>
        Logos.FirstOrDefault(l => l.Variant == variant && l.Rel == LogoRels.Default)
        ?? Logos.FirstOrDefault(l => l.Variant == variant)
        ?? Logos.FirstOrDefault(l => l.Variant == LogoVariants.Default && l.Rel == LogoRels.Default)
        ?? Logos.FirstOrDefault(l => l.Variant == LogoVariants.Default);

    public LeagueWordmark? WordmarkFor(string variant) =>
        Wordmarks.FirstOrDefault(w => w.Variant == variant)
        ?? Wordmarks.FirstOrDefault(w => w.Variant == LogoVariants.Default);

    public LeagueLogo UpsertLogo(string variant, string rel, string sourceKey, string sourceUrl, DateTimeOffset? sourceUpdatedAtUtc)
    {
        var existing = Logos.FirstOrDefault(l => l.Variant == variant && l.Rel == rel);
        if (existing is not null)
        {
            existing.PointAt(sourceKey, sourceUrl, sourceUpdatedAtUtc);
            return existing;
        }

        var logo = new LeagueLogo(variant, rel, ImageRef.FromSource(sourceKey, sourceUrl, sourceUpdatedAtUtc));
        Logos.Add(logo);
        return logo;
    }

    public LeagueWordmark UpsertWordmark(string variant)
    {
        var existing = Wordmarks.FirstOrDefault(w => w.Variant == variant);
        if (existing is not null)
            return existing;

        var wordmark = new LeagueWordmark(variant, ImageRef.Empty());
        Wordmarks.Add(wordmark);
        return wordmark;
    }
}
