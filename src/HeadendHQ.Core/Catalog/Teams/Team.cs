using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Teams;

public class Team : IEntity<int>, IExternalRef
{
    private Team() { }

    public Team(int leagueId, string displayName)
    {
        LeagueId = leagueId;
        DisplayName = displayName;
    }

    public int Id { get; init; }
    public int LeagueId { get; private set; }

    public string? Slug { get; private set; }
    public string? Abbreviation { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? ShortDisplayName { get; private set; }
    public string? Location { get; private set; }
    public string? Nickname { get; private set; }

    /// <summary>Hex without a leading '#', as ESPN supplies it.</summary>
    public string? PrimaryColorHex { get; private set; }
    public string? AlternateColorHex { get; private set; }

    public bool IsActive { get; private set; } = true;
    public bool IsFollowed { get; private set; }

    /// <summary>Which logo variant artwork should use for this team. See <see cref="LogoRels"/>.</summary>
    public string PreferredLogoRel { get; private set; } = LogoRels.OnSecondaryColor;

    /// <summary>
    /// When this team's logo variants were confirmed against a trustworthy source. A source's bulk
    /// listing may be wrong about them — ESPN's is, for the NFL — so the per-team confirmation is
    /// tracked separately. Logo addresses are stable, so this happens once per team, ever.
    /// </summary>
    public DateTimeOffset? LogosVerifiedAtUtc { get; private set; }

    public string? SourceKey { get; private set; }
    public string? ExternalId { get; private set; }

    public List<TeamLogo> Logos { get; private set; } = [];

    public void Describe(
        string displayName, string? shortDisplayName, string? slug, string? abbreviation,
        string? location, string? nickname, string? primaryColorHex, string? alternateColorHex, bool isActive)
    {
        DisplayName = displayName;
        ShortDisplayName = shortDisplayName;
        Slug = slug;
        Abbreviation = abbreviation;
        Location = location;
        Nickname = nickname;
        PrimaryColorHex = NormalizeColor(primaryColorHex) ?? PrimaryColorHex;
        AlternateColorHex = NormalizeColor(alternateColorHex) ?? AlternateColorHex;
        IsActive = isActive;
    }

    public void Follow(bool followed) => IsFollowed = followed;

    public void PreferLogo(string rel) => PreferredLogoRel = rel;

    public void OverrideColors(string? primaryColorHex, string? alternateColorHex)
    {
        if (primaryColorHex is not null) PrimaryColorHex = NormalizeColor(primaryColorHex);
        if (alternateColorHex is not null) AlternateColorHex = NormalizeColor(alternateColorHex);
    }

    public void MarkLogosVerified() => LogosVerifiedAtUtc = DateTimeOffset.UtcNow;

    public bool LogosNeedVerifying => LogosVerifiedAtUtc is null;

    public void TrackSource(string sourceKey, string externalId)
    {
        SourceKey = sourceKey;
        ExternalId = externalId;
    }

    /// <summary>
    /// The stored mark artwork should use. The plain <see cref="LogoRels.Default"/> comes before an
    /// arbitrary pick because it is the one address that is always right — the on-colour variants may
    /// not have been verified yet. <see cref="LogoSelection.ForTeam"/> applies the same order to a
    /// source's candidates before anything is downloaded.
    /// </summary>
    public TeamLogo? PreferredLogo() =>
        Logos.FirstOrDefault(l => l.Label == PreferredLogoRel)
        ?? Logos.FirstOrDefault(l => l.Label == LogoRels.OnSecondaryColor)
        ?? Logos.FirstOrDefault(l => l.Label == LogoRels.OnPrimaryColor)
        ?? Logos.FirstOrDefault(l => l.Label == LogoRels.Default)
        ?? Logos.FirstOrDefault(l => l.Label == LogoRels.Scoreboard)
        ?? Logos.FirstOrDefault();

    /// <summary>Drops every stored mark, so the next refresh downloads them again.</summary>
    public void ClearLogos() => Logos.Clear();

    public TeamLogo UpsertLogo(string? label, int imageId, ImageOrigin origin = ImageOrigin.Fetched) =>
        Catalog.Logos.Upsert(
            Logos, LogoVariants.Default, label, imageId, origin,
            () => new TeamLogo(LogoVariants.Default, label, imageId, origin));

    private static string? NormalizeColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        var trimmed = hex.Trim().TrimStart('#').ToLowerInvariant();
        return trimmed.Length is 3 or 6 or 8 ? trimmed : null;
    }
}
