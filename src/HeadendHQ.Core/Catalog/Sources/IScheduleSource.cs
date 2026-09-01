namespace HeadendHQ.Core.Catalog.Sources;

/// <summary>Where events come from. Several may be registered; results are merged by external id.</summary>
public interface IScheduleSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<ScheduledEventDescriptor>> GetEventsAsync(ScheduleQuery query, CancellationToken ct);
}
