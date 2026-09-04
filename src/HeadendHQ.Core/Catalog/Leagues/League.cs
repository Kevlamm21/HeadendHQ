using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Leagues;

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
        Logos.FirstOrDefault(l => l.Variant == variant && l.Label == LogoRels.Default)
        ?? Logos.FirstOrDefault(l => l.Variant == variant)
        ?? Logos.FirstOrDefault(l => l.Variant == LogoVariants.Default && l.Label == LogoRels.Default)
        ?? Logos.FirstOrDefault(l => l.Variant == LogoVariants.Default);

    public LeagueWordmark? WordmarkFor(string variant) =>
        Wordmarks.FirstOrDefault(w => w.Variant == variant)
        ?? Wordmarks.FirstOrDefault(w => w.Variant == LogoVariants.Default);

    public LeagueLogo UpsertLogo(
        string variant, string? label, int imageId, ImageOrigin origin = ImageOrigin.Fetched) =>
        Catalog.Logos.Upsert(
            Logos, variant, label, imageId, origin,
            () => new LeagueLogo(variant, label, imageId, origin));

    public LeagueWordmark UpsertWordmark(string variant, int imageId)
    {
        if (Wordmarks.FirstOrDefault(w => w.Variant == variant) is { } existing)
        {
            existing.PointAt(imageId, ImageOrigin.Manual);
            return existing;
        }

        var wordmark = new LeagueWordmark(variant, imageId);
        Wordmarks.Add(wordmark);
        return wordmark;
    }
}
