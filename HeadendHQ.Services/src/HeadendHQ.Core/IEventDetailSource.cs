using HeadendHQ.Core.Events;

namespace HeadendHQ.Core;

public record EventKey(string SportSlug, string LeagueSlug, string EventExternalId, DateOnly Date);

public interface IEventDetailSource
{
    Task<IReadOnlyDictionary<string, EventDetailRequest>> GetDetailsAsync(
        IReadOnlyList<EventKey> keys, CancellationToken ct);
}
