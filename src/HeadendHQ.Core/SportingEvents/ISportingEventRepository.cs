namespace HeadendHQ.Core.SportingEvents;

public interface ISportingEventRepository
{
    Task<List<SportingEvent>> GetAsync(DateTime? from, DateTime? to, Sport? sport, CancellationToken ct = default);
    Task<SportingEvent?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SportingEvent?> GetByTitleAsync(string title, CancellationToken ct = default);
    Task<SportingEvent?> FindByProviderTitleStartAsync(string provider, string title, DateTime startUtc, CancellationToken ct = default);
    Task AddAsync(SportingEvent evt, CancellationToken ct = default);
    Task DeleteAsync(SportingEvent evt, CancellationToken ct = default);
    Task DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
