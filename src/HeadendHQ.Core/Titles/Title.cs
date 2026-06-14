using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles;

public class Title : Entity<Guid>
{
    private Title() { }

    public Title(TitleRequest request)
    {
        Name = request.Name;
        Type = request.Type;
        StreamingService = request.StreamingService;
        Provider = request.Provider;
        ExternalId = request.ExternalId;
        EventUrl = request.EventUrl;
        Metadata = request.Metadata;
        StartUtc = request.StartUtc;
        EndUtc = request.EndUtc ?? request.StartUtc?.AddHours(3);
        RecordEvent(new TitleCreated(Id, StartUtc));
    }

    public override Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public TitleType Type { get; set; }
    public StreamingService StreamingService { get; set; }
    public string? ExternalId { get; set; }
    public string? EventUrl { get; set; }
    public string? AdbCommand { get; set; }
    public string? VodLauncherPath { get; set; }
    public bool ArtworkCreated { get; set; }
    public string Provider { get; set; } = string.Empty;
    public TitleMetadata? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLive { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }

    public void Update(UpdateTitleRequest request)
    {
        if (request.Name is not null) Name = request.Name;
        if (request.Type is not null) Type = request.Type.Value;
        if (request.StreamingService is not null) StreamingService = request.StreamingService.Value;
        if (request.StartUtc is not null) StartUtc = request.StartUtc;
        if (request.EndUtc is not null) EndUtc = request.EndUtc;
        if (request.AdbCommand is not null) AdbCommand = request.AdbCommand == "" ? null : request.AdbCommand;
        if (request.VodLauncherPath is not null) VodLauncherPath = request.VodLauncherPath == "" ? null : request.VodLauncherPath;
        if (request.ArtworkCreated is true) ArtworkCreated = true;

        if (request.EventUrl is not null)
        {
            var newUrl = request.EventUrl == "" ? null : request.EventUrl;
            if (EventUrl != newUrl)
            {
                EventUrl = newUrl;
                AdbCommand = null;
            }
        }

        var metadataChanged = request.Metadata is not null || request.IsLive is not null;
        if (request.Metadata is not null) Metadata = request.Metadata;
        if (request.IsLive is not null) IsLive = request.IsLive.Value;

        if (metadataChanged)
            RecordEvent(new TitleMetadataUpdated(Id));

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public record TitleCreated(Guid TitleId, DateTime? StartUtc) : IEvent;

public record TitleMetadataUpdated(Guid TitleId) : IEvent;

public record TitleRequest
{
    public required string Name { get; init; }
    public required TitleType Type { get; init; }
    public required StreamingService StreamingService { get; init; }
    public string? ExternalId { get; init; }
    public string? EventUrl { get; init; }
    public required string Provider { get; init; }
    public TitleMetadata? Metadata { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
}

public record UpdateTitleRequest
{
    public string? Name { get; init; }
    public TitleType? Type { get; init; }
    public StreamingService? StreamingService { get; init; }
    public string? EventUrl { get; init; }
    public string? AdbCommand { get; init; }
    public string? VodLauncherPath { get; init; }
    public bool? ArtworkCreated { get; init; }
    public bool? IsLive { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
    public TitleMetadata? Metadata { get; init; }
}