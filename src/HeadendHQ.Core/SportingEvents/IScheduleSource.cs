namespace HeadendHQ.Core.SportingEvents;

public interface IScheduleSource
{
    Sport Sport { get; }
    Task FetchEventsAsync(CancellationToken ct);
}
