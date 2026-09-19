using HeadendHQ.Core.Events;

namespace HeadendHQ.Core;

public record ScheduleQuery(DateOnly From, DateOnly To, IReadOnlyList<string> LeagueSlugs);

public interface IScheduleSource
{
    string SourceKey { get; }

    Task<IReadOnlyList<SportingEventRequest>> GetEventsAsync(ScheduleQuery query, CancellationToken ct);
}
