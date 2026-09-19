using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Broadcasters;

public enum BroadcasterKind
{
    Unknown,
    Television,
    Streaming
}

public record BroadcasterRequest(
    string ExternalId,
    string Slug,
    string Name,
    string? ShortName = null,
    string? CallLetters = null,
    IReadOnlyList<LogoRequest>? Logos = null);

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

    public bool IsSubscribed { get; private set; }

    public DateTimeOffset? DetailFetchedAtUtc { get; private set; }

    public List<string> Aliases { get; private set; } = [];

    public string? SourceKey { get; private set; }
    public string? ExternalId { get; private set; }

    public List<BroadcasterLogo> Logos { get; private set; } = [];

    public List<StreamingAssignment> StreamingAssignments { get; private set; } = [];

    public void Describe(string name, string? shortName, string? callLetters, BroadcasterKind kind)
    {
        Name = name;
        ShortName = shortName;
        CallLetters = callLetters;
        if (kind is not BroadcasterKind.Unknown) Kind = kind;
    }

    public void MarkDetailFetched() => DetailFetchedAtUtc = DateTimeOffset.UtcNow;

    public bool NeedsDetail => DetailFetchedAtUtc is null;

    public void SetStreamingAssignments(IEnumerable<StreamingAssignment> assignments)
    {
        var list = assignments.ToList();

        if (list.Where(a => a.StreamingServiceId is not null).GroupBy(a => a.StreamingServiceId).Any(g => g.Count() > 1))
            throw new InvalidOperationException($"Broadcaster '{Slug}' can be assigned each streaming service only once.");

        if (list.Count(a => a.StreamingServiceId is null) > 1)
            throw new InvalidOperationException($"Broadcaster '{Slug}' can have only one 'no stream available' assignment.");

        StreamingAssignments.Clear();
        StreamingAssignments.AddRange(list);
        IsSubscribed = StreamingAssignments.Count > 0;
    }

    public void TrackSource(string sourceKey, string externalId)
    {
        SourceKey = sourceKey;
        ExternalId = externalId;
    }

    public BroadcasterLogo? SelectedLogo() =>
        Catalog.Logos.Selected(Logos, LogoVariants.Default, LogoPolicy.Broadcaster);

    public bool HasFetchedLogos => Catalog.Logos.HasFetched(Logos);

    public IReadOnlyList<int> StoreFetchedLogos(LogoDownload download) =>
        Catalog.Logos.StoreFetched(
            Logos, LogoVariants.Default, download, LogoPolicy.Broadcaster,
            f => new BroadcasterLogo(LogoVariants.Default, f.Label, f.ImageId, ImageOrigin.Fetched));
}
