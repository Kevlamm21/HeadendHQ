using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog;

public enum BroadcasterKind
{
    Unknown,
    Television,
    Streaming
}

/// <summary>
/// A network or streaming service. Entirely source-derived — there is no hand-written manifest of
/// "the ones we know about", because guessing that list is what left ABC, FOX and every local station
/// missing. Rows arrive from ESPN's media index and from the schedule, unsubscribed; the launch
/// package and mapping are chosen per row afterwards.
/// </summary>
public class Broadcaster : IEntity<int>
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

    /// <summary>Android package used to launch this service. Consumed by the deep-link work.</summary>
    public string? AndroidPackage { get; private set; }

    public bool IsSubscribed { get; private set; }

    /// <summary>
    /// A local station rather than a national network — ESPN reports these as the carrier of a
    /// regional game. Set from a W/K call-sign signal, never from the absence of a lineup match, so
    /// <c>fox-sports-1</c> and <c>tnt</c> stay national. An affiliate not in the lineup is kept but
    /// produces nothing while unsubscribed.
    /// </summary>
    public bool IsAffiliate { get; private set; }

    /// <summary>
    /// Another broadcaster to stand in for this one. ESPN reports the local affiliate that actually
    /// carries a game — KTRK, FOX32, WBAL-TV — and those are neither launchable nor logoed. Pointing
    /// one at ESPN means the poster shows the ESPN mark and the launcher opens the ESPN app.
    /// </summary>
    public int? MapsToBroadcasterId { get; private set; }

    /// <summary>
    /// An IPTV lineup guide number ("19.1") to tune instead of launching an app. Unlike
    /// <see cref="MapsToBroadcasterId"/> this keeps the broadcaster's own identity: ABC on an aerial
    /// is still ABC, so the poster keeps the ABC mark.
    /// </summary>
    public string? IptvGuideNumber { get; private set; }

    /// <summary>
    /// When the source's own record for this broadcaster was last read. Null means we have never
    /// looked it up — which is true of every seeded row until the schedule first names it.
    /// </summary>
    public DateTimeOffset? DetailFetchedAtUtc { get; private set; }

    /// <summary>
    /// Extra slugs that mean this broadcaster. ESPN splits a single service across many product
    /// slugs (espn, espn2, espnu, espn-unlimited, espnplus), and simulcasts land on the broadcast
    /// network, so the mapping has to be data rather than a prefix match.
    /// </summary>
    public List<string> Aliases { get; private set; } = [];

    public List<ExternalRef> ExternalRefs { get; private set; } = [];
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

    /// <summary>
    /// Points this broadcaster at a stand-in. The two targets are mutually exclusive — a game is
    /// either launched in another app or tuned on an aerial — and a broadcaster can never map to
    /// itself, which would be an immediate cycle.
    /// </summary>
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

    public void TrackSource(string sourceKey, string externalId) =>
        ExternalRefs.Track(sourceKey, externalId);

    public BroadcasterLogo? LogoFor(string variant) =>
        Logos.FirstOrDefault(l => l.Variant == variant)
        ?? Logos.FirstOrDefault(l => l.Variant == LogoVariants.Default)
        ?? Logos.FirstOrDefault();

    /// <summary>
    /// The logo artwork should use: the dark variant first — ESPN's networks publish a light-on-dark
    /// mark that reads on a team-coloured card — then the default, then whatever exists.
    /// </summary>
    public BroadcasterLogo? PreferredLogo() =>
        Logos.FirstOrDefault(l => l.Variant == LogoRels.Dark)
        ?? Logos.FirstOrDefault(l => l.Variant == LogoVariants.Default)
        ?? Logos.FirstOrDefault();

    public BroadcasterLogo UpsertLogo(string variant, string sourceKey, string sourceUrl, DateTimeOffset? sourceUpdatedAtUtc)
    {
        var existing = Logos.FirstOrDefault(l => l.Variant == variant);
        if (existing is not null)
        {
            existing.PointAt(sourceKey, sourceUrl, sourceUpdatedAtUtc);
            return existing;
        }

        var logo = new BroadcasterLogo(variant, ImageRef.FromSource(sourceKey, sourceUrl, sourceUpdatedAtUtc));
        Logos.Add(logo);
        return logo;
    }
}
