namespace HeadendHQ.Core;

public interface IScheduleSource
{
    Task<int> FetchEventsAsync(CancellationToken ct);
}
