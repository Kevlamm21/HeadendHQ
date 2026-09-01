namespace HeadendHQ.Core.Catalog.Sources;

/// <summary>
/// The second, optional call per event: venue, game note, series standing and cast candidates.
/// Split from <see cref="IScheduleSource"/> because it is fetched once per event ever, whereas the
/// schedule is re-read every night.
/// </summary>
public interface IEventDetailSource
{
    string SourceKey { get; }

    Task<EventDetailDescriptor?> GetDetailAsync(EventKey key, CancellationToken ct);
}
