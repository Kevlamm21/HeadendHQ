using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Broadcasters;

public enum BroadcasterKind
{
    Unknown,
    Television,
    Streaming
}

public class Broadcaster : IEntity<int>, IExternalRef
{
    private Broadcaster() { }

    public Broadcaster(string slug, string name)
    {
        Slug = slug;
        Name = name;
    }

    public int Id { get; init; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ShortName { get; private set; }
    public string? CallLetters { get; private set; }
    public BroadcasterKind Kind { get; private set; }

    public string? AndroidPackage { get; private set; }

    public bool IsSubscribed { get; private set; }

    public bool IsAffiliate { get; private set; }

    public int? MapsToBroadcasterId { get; private set; }

    public string? IptvGuideNumber { get; private set; }

    public DateTimeOffset? DetailFetchedAtUtc { get; private set; }

    public List<string> Aliases { get; private set; } = [];

    public string? SourceKey { get; private set; }
    public string? ExternalId { get; private set; }

    public List<BroadcasterLogo> Logos { get; private set; } = [];

    public void Describe(string name, string? shortName, string? callLetters, BroadcasterKind kind)
    {
        Name = name;
        ShortName = shortName;
        CallLetters = callLetters;
        if (kind is not BroadcasterKind.Unknown) Kind = kind;
    }

    public void Subscribe(bool subscribed) => IsSubscribed = subscribed;

    public void SetAffiliate(bool isAffiliate) => IsAffiliate = isAffiliate;

    public void SetAndroidPackage(string? androidPackage) => AndroidPackage = androidPackage;

    public void MarkDetailFetched() => DetailFetchedAtUtc = DateTimeOffset.UtcNow;

    public bool NeedsDetail => DetailFetchedAtUtc is null;

    public void SetMapping(int? mapsToBroadcasterId, string? iptvGuideNumber)
    {
        if (mapsToBroadcasterId == Id)
            throw new InvalidOperationException($"Broadcaster '{Slug}' cannot map to itself.");

        var channel = string.IsNullOrWhiteSpace(iptvGuideNumber) ? null : iptvGuideNumber.Trim();

        if (mapsToBroadcasterId is not null && channel is not null)
            throw new InvalidOperationException(
                $"Broadcaster '{Slug}' can map to another broadcaster or an IPTV channel, not both.");

        MapsToBroadcasterId = mapsToBroadcasterId;
        IptvGuideNumber = channel;
    }

    public void AddAlias(string alias)
    {
        if (!Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
            Aliases.Add(alias);
    }

    public bool Matches(string slug) =>
        Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)
        || Aliases.Contains(slug, StringComparer.OrdinalIgnoreCase);

    public void TrackSource(string sourceKey, string externalId)
    {
        SourceKey = sourceKey;
        ExternalId = externalId;
    }

    public BroadcasterLogo? PreferredLogo() =>
        Logos.FirstOrDefault(l => l.Label == LogoRels.Dark)
        ?? Logos.FirstOrDefault(l => l.Label == LogoRels.Default)
        ?? Logos.FirstOrDefault();

    public BroadcasterLogo UpsertLogo(string? label, int imageId, ImageOrigin origin = ImageOrigin.Fetched) =>
        Catalog.Logos.Upsert(
            Logos, LogoVariants.Default, label, imageId, origin,
            () => new BroadcasterLogo(LogoVariants.Default, label, imageId, origin));
}
