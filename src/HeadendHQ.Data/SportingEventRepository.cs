using HeadendHQ.Core.SportingEvents;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

public class SportingEventRepository(AppDbContext db) : ISportingEventRepository
{
    public async Task<List<SportingEvent>> GetAsync(DateTime? from, DateTime? to, Sport? sport, CancellationToken ct = default)
    {
        var query = db.SportingEvents.AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.StartUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartUtc <= to.Value);

        if (sport.HasValue)
            query = query.Where(e => e.Sport == sport.Value);

        return await query.OrderBy(e => e.StartUtc).ToListAsync(ct);
    }

    public async Task<SportingEvent?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.SportingEvents.FindAsync([id], ct);
    }

    public async Task<SportingEvent?> GetByTitleAsync(string title, CancellationToken ct = default)
    {
        return await db.SportingEvents
            .Where(e => e.Title.ToLower() == title.ToLower())
            .OrderBy(e => e.StartUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SportingEvent?> FindByProviderTitleStartAsync(string provider, string title, DateTime startUtc, CancellationToken ct = default)
    {
        return await db.SportingEvents
            .FirstOrDefaultAsync(e =>
                e.Provider == provider &&
                e.Title == title &&
                e.StartUtc == startUtc, ct);
    }

    public async Task AddAsync(SportingEvent evt, CancellationToken ct = default)
    {
        db.SportingEvents.Add(evt);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(SportingEvent evt, CancellationToken ct = default)
    {
        db.SportingEvents.Remove(evt);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var expired = await db.SportingEvents
            .Where(e => e.StartUtc < cutoff)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return;

        db.SportingEvents.RemoveRange(expired);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }
}
