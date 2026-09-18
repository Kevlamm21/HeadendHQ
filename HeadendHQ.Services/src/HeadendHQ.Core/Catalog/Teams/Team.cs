using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Teams;

public class Team : IEntity<int>, IExternalRef
{
    private Team() { }

    public Team(int leagueId, string displayName, bool isFollowed = false)
    {
        LeagueId = leagueId;
        DisplayName = displayName;
        IsFollowed = isFollowed;
    }

    public int Id { get; init; }
    public int LeagueId { get; private set; }

    public string? Slug { get; private set; }
    public string? Abbreviation { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? ShortDisplayName { get; private set; }
    public string? Location { get; private set; }
    public string? Nickname { get; private set; }

    public string? PrimaryColorHex { get; private set; }
    public string? AlternateColorHex { get; private set; }

    public bool IsActive { get; private set; } = true;
    public bool IsFollowed { get; private set; }

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

    public TeamLogo? SelectedLogo() => Catalog.Logos.Selected(Logos, LogoVariants.Default, LogoPolicy.Team);

    public bool HasFetchedLogos => Catalog.Logos.HasFetched(Logos);

    public void ClearLogos() => Logos.Clear();

    public IReadOnlyList<int> StoreFetchedLogos(LogoDownload download) =>
        Catalog.Logos.StoreFetched(
            Logos, LogoVariants.Default, download, LogoPolicy.Team,
            f => new TeamLogo(LogoVariants.Default, f.Label, f.ImageId, ImageOrigin.Fetched));

    public TeamLogo AddUploadedLogo(int imageId) =>
        Catalog.Logos.AddUpload(
            Logos, LogoVariants.Default, imageId,
            () => new TeamLogo(LogoVariants.Default, null, imageId, ImageOrigin.Manual));

    public TeamLogo SelectLogo(int logoId) => Catalog.Logos.Select(Logos, logoId);

    public int RemoveLogo(int logoId) => Catalog.Logos.Remove(Logos, logoId, LogoPolicy.Team);

    private static string? NormalizeColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        var trimmed = hex.Trim().TrimStart('#').ToLowerInvariant();
        return trimmed.Length is 3 or 6 or 8 ? trimmed : null;
    }
}
