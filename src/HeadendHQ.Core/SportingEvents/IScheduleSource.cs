namespace HeadendHQ.Core.SportingEvents;

public interface IScheduleSource
{
    Sport Sport { get; }
    Task<int> FetchEventsAsync(CancellationToken ct);
}
