using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Leagues;

public class League : Entity<int>, IExternalRef
{
    private League() { }

    public League(int sportId, string slug, string name)
    {
        SportId = sportId;
        Slug = slug;
        Name = name;
    }

    public override int Id { get; init; }
    public int SportId { get; private set; }

    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Abbreviation { get; private set; }
    public string? ShortName { get; private set; }
    public bool IsFollowed { get; private set; }
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
    public void Follow(bool followed)
    {
        if (followed && !IsFollowed)
            RecordEvent(new LeagueFollowed(Id));

        IsFollowed = followed;
    }

    public void MarkTeamsRefreshed() => TeamsRefreshedAtUtc = DateTimeOffset.UtcNow;

    public void TrackSource(string sourceKey, string externalId)
    {
        SourceKey = sourceKey;
        ExternalId = externalId;
    }

    public LeagueLogo? SelectedLogo(string variant = LogoVariants.Default) =>
        Catalog.Logos.Selected(Logos, variant, LogoPolicy.League)
        ?? Catalog.Logos.Selected(Logos, LogoVariants.Default, LogoPolicy.League);

    public bool HasFetchedLogos => Catalog.Logos.HasFetched(Logos);

    public LeagueWordmark? WordmarkFor(string variant) =>
        Wordmarks.FirstOrDefault(w => w.Variant == variant)
        ?? Wordmarks.FirstOrDefault(w => w.Variant == LogoVariants.Default);

    public IReadOnlyList<int> StoreFetchedLogos(LogoDownload download) =>
        Catalog.Logos.StoreFetched(
            Logos, LogoVariants.Default, download, LogoPolicy.League,
            f => new LeagueLogo(LogoVariants.Default, f.Label, f.ImageId, ImageOrigin.Fetched));

    public LeagueLogo AddUploadedLogo(string variant, int imageId)
    {
        var logo = Catalog.Logos.AddUpload(
            Logos, variant, imageId,
            () => new LeagueLogo(variant, null, imageId, ImageOrigin.Manual));

        if (variant != LogoVariants.Default)
            Catalog.Logos.EnsureSelected(Logos, variant, LogoPolicy.League);

        return logo;
    }

    public LeagueLogo SelectLogo(int logoId) => Catalog.Logos.Select(Logos, logoId);

    public int RemoveLogo(int logoId) => Catalog.Logos.Remove(Logos, logoId, LogoPolicy.League);

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

public record LeagueFollowed(int LeagueId) : IEvent;
