namespace HeadendHQ.Core.Catalog.Sources;

public interface IScheduleSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<ScheduledEventDescriptor>> GetEventsAsync(ScheduleQuery query, CancellationToken ct);
}
