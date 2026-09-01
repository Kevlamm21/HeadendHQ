using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles;

/// <summary>
/// The type-agnostic production artifact: one thing to make a VOD launcher for, write an NFO for,
/// compose artwork for, and launch.
/// <para>
/// A title carries no domain knowledge of what it depicts. Whatever produced it — a
/// <see cref="Events.SportingEvent"/> today, a video game or a movie later — resolves its own world
/// into the flat fields here. That is what lets the NFO writer, the artwork composer and the ADB
/// mapper stay simple and shared, instead of growing a branch per content type.
/// </para>
/// </summary>
public class Title : Entity<Guid>
{
    private Title() { }

    public Title(TitleRequest request)
    {
        Name = request.Name;
        Type = request.Type;
        LaunchSlug = request.LaunchSlug;
        EventUrl = request.EventUrl;
        Metadata = request.Metadata;
        Artwork = request.Artwork ?? TitleArtwork.Empty();
        StartUtc = request.StartUtc;
        EndUtc = request.EndUtc ?? request.StartUtc?.AddHours(3);

        SetCast(request.Cast ?? []);
        RecordEvent(new TitleCreated(Id, StartUtc));
    }

    public override Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;
    public TitleType Type { get; private set; }

    /// <summary>
    /// Which app launches this, as a broadcaster slug. A slug rather than an enum so a service ESPN
    /// starts carrying tomorrow does not need a release.
    /// </summary>
    public string? LaunchSlug { get; private set; }

    public string? EventUrl { get; private set; }
    public string? AdbCommand { get; private set; }

    public string? VodLauncherPath { get; private set; }
    public bool ArtworkCreated { get; private set; }
    public bool IsVideoCreated { get; private set; }
    public string? LiveJobId { get; private set; }

    public TitleMetadata? Metadata { get; private set; }
    public TitleArtwork Artwork { get; private set; } = TitleArtwork.Empty();
    public List<TitleCastMember> Cast { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsLive { get; private set; }
    public DateTime? StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    public void Update(UpdateTitleRequest request)
    {
        var metadataChanged = request.Metadata is not null
            || request.Cast is not null
            || request.IsLive is not null
            || (request.StartUtc is not null && request.StartUtc != StartUtc);

        if (request.Name is not null) Name = request.Name;
        if (request.Type is not null) Type = request.Type.Value;
        if (request.LaunchSlug is not null) LaunchSlug = Blank(request.LaunchSlug);
        if (request.StartUtc is not null) StartUtc = request.StartUtc;
        if (request.EndUtc is not null) EndUtc = request.EndUtc;
        if (request.VodLauncherPath is not null) VodLauncherPath = Blank(request.VodLauncherPath);
        if (request.ArtworkCreated is true) ArtworkCreated = true;
        if (request.Metadata is not null) Metadata = request.Metadata;
        if (request.Artwork is not null)
        {
            Artwork = request.Artwork;
            ArtworkCreated = false;
        }
        if (request.Cast is not null) SetCast(request.Cast);
        if (request.IsLive is not null) IsLive = request.IsLive.Value;

        // The ADB command is derived from the URL, so a new URL invalidates it.
        if (request.EventUrl is not null)
        {
            var newUrl = Blank(request.EventUrl);
            if (EventUrl != newUrl)
            {
                EventUrl = newUrl;
                AdbCommand = null;
            }
        }

        if (request.AdbCommand is not null) AdbCommand = Blank(request.AdbCommand);

        if (metadataChanged)
            RecordEvent(new TitleMetadataUpdated(Id));

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAdbCommand(string? adbCommand)
    {
        AdbCommand = Blank(adbCommand);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetVodLauncherPath(string? path)
    {
        VodLauncherPath = Blank(path);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkArtworkCreated() => ArtworkCreated = true;

    public void MarkVideoCreated(bool created) => IsVideoCreated = created;

    public void SetLiveJobId(string? jobId) => LiveJobId = jobId;

    public void MarkLive(bool isLive)
    {
        IsLive = isLive;
        UpdatedAt = DateTimeOffset.UtcNow;
        RecordEvent(new TitleMetadataUpdated(Id));
    }

    /// <summary>
    /// Announces that this title is going away, so whatever produced it can let go of the reference
    /// and produce a replacement if the underlying thing is still due. Call before removing.
    /// </summary>
    public void MarkDeleted() => RecordEvent(new TitleDeleted(Id));

    private void SetCast(IEnumerable<TitleCastRequest> cast)
    {
        Cast.Clear();
        var order = 0;

        foreach (var member in cast)
            Cast.Add(new TitleCastMember(member.Name, member.Role, member.HeadshotImageId, order++));
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public record TitleCreated(Guid TitleId, DateTime? StartUtc) : IEvent;

public record TitleMetadataUpdated(Guid TitleId) : IEvent;

public record TitleDeleted(Guid TitleId) : IEvent;

public record TitleCastRequest(string Name, string? Role, int? HeadshotImageId);

public record TitleRequest
{
    public required string Name { get; init; }
    public required TitleType Type { get; init; }
    public string? LaunchSlug { get; init; }
    public string? EventUrl { get; init; }
    public TitleMetadata? Metadata { get; init; }
    public TitleArtwork? Artwork { get; init; }
    public IReadOnlyList<TitleCastRequest>? Cast { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
}

public record UpdateTitleRequest
{
    public string? Name { get; init; }
    public TitleType? Type { get; init; }
    public string? LaunchSlug { get; init; }
    public string? EventUrl { get; init; }
    public string? AdbCommand { get; init; }
    public string? VodLauncherPath { get; init; }
    public bool? ArtworkCreated { get; init; }
    public bool? IsLive { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
    public TitleMetadata? Metadata { get; init; }
    public TitleArtwork? Artwork { get; init; }
    public IReadOnlyList<TitleCastRequest>? Cast { get; init; }
}
