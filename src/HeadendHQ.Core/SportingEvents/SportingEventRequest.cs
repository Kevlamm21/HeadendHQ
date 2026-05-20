namespace HeadendHQ.Core.SportingEvents;

public record SportingEventRequest
{
    public required string Title { get; init; }
    public required Sport Sport { get; init; }
    public required StreamingService StreamingService { get; init; }
    public required string EventUrl { get; init; }
    public required string Provider { get; init; }
    public required DateTime StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
}
