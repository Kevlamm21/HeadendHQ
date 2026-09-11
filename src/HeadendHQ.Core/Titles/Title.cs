using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles;

public class Title : Entity<Guid>
{
    private Title() { }

    public Title(TitleRequest request)
    {
        Name = request.Name;
        Type = request.Type;
        SourceId = request.SourceId;
        LaunchSlug = request.LaunchSlug;
        EventUrl = request.EventUrl;
        StartUtc = request.StartUtc;
        EndUtc = request.EndUtc ?? request.StartUtc?.AddHours(3);
        Plot = request.Plot;
        Tagline = request.Tagline;
        Studio = request.Studio;
        Genres = request.Genres is not null ? [.. request.Genres] : [];
        Sets = request.Sets is not null ? [.. request.Sets] : [];
        ContentRating = request.ContentRating;
        UniqueId = request.UniqueId;
        Cast = request.Cast is not null ? [.. request.Cast] : [];

        RecordEvent(new TitleCreated(Id, StartUtc));
    }

    public override Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public TitleType Type { get; private set; }
    public Guid? SourceId { get; private set; }
    public string? LaunchSlug { get; private set; }
    public string? EventUrl { get; private set; }
    public string? AdbCommand { get; private set; }
    public string? VodLauncherPath { get; private set; }
    public bool ArtworkCreated { get; private set; }
    public bool IsVideoCreated { get; private set; }
    public string? LiveJobId { get; private set; }
    public string? Plot { get; private set; }
    public string? Tagline { get; private set; }
    public string? Studio { get; private set; }
    public List<string> Genres { get; private set; } = [];
    public List<string> Sets { get; private set; } = [];
    public string? ContentRating { get; private set; }
    public string? UniqueId { get; private set; }
    public List<TitleCastEntry> Cast { get; private set; } = [];
    public int? PosterImageId { get; private set; }
    public int? BackgroundImageId { get; private set; }
    public int? ThumbnailImageId { get; private set; }
    public int? ClearLogoImageId { get; private set; }

    public bool HasArtwork =>
        PosterImageId is not null || BackgroundImageId is not null
        || ThumbnailImageId is not null || ClearLogoImageId is not null;

    public TitleProductionProfile Production => TitleProductionProfile.For(Type);

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsLive { get; private set; }
    public DateTime? StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    public void Update(UpdateTitleRequest request)
    {
        var nfoChanged = request.IsLive is not null
            || (request.StartUtc is not null && request.StartUtc != StartUtc);

        if (request.Plot is not null) { Plot = request.Plot; nfoChanged = true; }
        if (request.Tagline is not null) { Tagline = request.Tagline; nfoChanged = true; }
        if (request.Studio is not null) { Studio = request.Studio; nfoChanged = true; }
        if (request.Genres is not null) { Genres = [.. request.Genres]; nfoChanged = true; }
        if (request.Sets is not null) { Sets = [.. request.Sets]; nfoChanged = true; }
        if (request.ContentRating is not null) { ContentRating = request.ContentRating; nfoChanged = true; }
        if (request.UniqueId is not null) { UniqueId = request.UniqueId; nfoChanged = true; }
        if (request.Cast is not null) { Cast = [.. request.Cast]; nfoChanged = true; }
        if (request.Name is not null) Name = request.Name;
        if (request.Type is not null) Type = request.Type.Value;
        if (request.SourceId is not null) SourceId = request.SourceId;
        if (request.LaunchSlug is not null) LaunchSlug = Blank(request.LaunchSlug);
        if (request.StartUtc is not null) StartUtc = request.StartUtc;
        if (request.EndUtc is not null) EndUtc = request.EndUtc;
        if (request.VodLauncherPath is not null) VodLauncherPath = Blank(request.VodLauncherPath);
        if (request.ArtworkCreated is true) ArtworkCreated = true;
        if (request.IsLive is not null) IsLive = request.IsLive.Value;

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

        if (nfoChanged)
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

    public void SetRenderedArtwork(int? poster, int? background, int? thumbnail, int? clearLogo)
    {
        PosterImageId = poster;
        BackgroundImageId = background;
        ThumbnailImageId = thumbnail;
        ClearLogoImageId = clearLogo;
        ArtworkCreated = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        RecordEvent(new TitleMetadataUpdated(Id));
    }

    public void MarkVideoCreated(bool created) => IsVideoCreated = created;

    public void SetLiveJobId(string? jobId) => LiveJobId = jobId;

    public void MarkLive(bool isLive)
    {
        IsLive = isLive;
        UpdatedAt = DateTimeOffset.UtcNow;
        RecordEvent(new TitleMetadataUpdated(Id));
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public record TitleCreated(Guid TitleId, DateTime? StartUtc) : IEvent;
public record TitleMetadataUpdated(Guid TitleId) : IEvent;

public record TitleRequest
{
    public required string Name { get; init; }
    public required TitleType Type { get; init; }
    public Guid? SourceId { get; init; }
    public string? LaunchSlug { get; init; }
    public string? EventUrl { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
    public string? Plot { get; init; }
    public string? Tagline { get; init; }
    public string? Studio { get; init; }
    public IReadOnlyList<string>? Genres { get; init; }
    public IReadOnlyList<string>? Sets { get; init; }
    public string? ContentRating { get; init; }
    public string? UniqueId { get; init; }
    public IReadOnlyList<TitleCastEntry>? Cast { get; init; }
}

public record UpdateTitleRequest
{
    public string? Name { get; init; }
    public TitleType? Type { get; init; }
    public Guid? SourceId { get; init; }
    public string? LaunchSlug { get; init; }
    public string? EventUrl { get; init; }
    public string? AdbCommand { get; init; }
    public string? VodLauncherPath { get; init; }
    public bool? ArtworkCreated { get; init; }
    public bool? IsLive { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
    public string? Plot { get; init; }
    public string? Tagline { get; init; }
    public string? Studio { get; init; }
    public IReadOnlyList<string>? Genres { get; init; }
    public IReadOnlyList<string>? Sets { get; init; }
    public string? ContentRating { get; init; }
    public string? UniqueId { get; init; }
    public IReadOnlyList<TitleCastEntry>? Cast { get; init; }
}

public enum TitleType
{
    SportingEvent,
    VideoGame,
    Movie,
    TvEpisode
}
public class TitleCastEntry
{
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int? HeadshotImageId { get; set; }
}
