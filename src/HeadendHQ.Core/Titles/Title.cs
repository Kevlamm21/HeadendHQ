using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles;

public class Title : IEntity<Guid>
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
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public TitleType Type { get; set; }
    public StreamingService StreamingService { get; set; }
    public string? ExternalId { get; set; }
    public string? EventUrl { get; set; }
    public string? AdbCommand { get; set; }
    public string? DummyVideoPath { get; set; }
    public string? PosterPath { get; set; }
    public string Provider { get; set; } = string.Empty;
    public TitleMetadata? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLive { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }

    public void Update(TitleRequest request)
    {
        Name = request.Name;
        Type = request.Type;
        StreamingService = request.StreamingService;
        Provider = request.Provider;
        Metadata = request.Metadata;
        StartUtc = request.StartUtc;
        EndUtc = request.EndUtc ?? request.StartUtc?.AddHours(3);
        UpdatedAt = DateTimeOffset.UtcNow;

        var newUrl = string.IsNullOrEmpty(request.EventUrl) ? EventUrl : request.EventUrl;
        if (EventUrl != newUrl)
        {
            EventUrl = newUrl;
            AdbCommand = null;
        }

        if (request.DummyVideoPath is not null)
            DummyVideoPath = string.IsNullOrEmpty(request.DummyVideoPath) ? null : request.DummyVideoPath;

        if (request.PosterPath is not null)
            PosterPath = string.IsNullOrEmpty(request.PosterPath) ? null : request.PosterPath;
    }
}

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
    public string? DummyVideoPath { get; init; }
    public string? PosterPath { get; init; }
}