using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Events.Specifications;

public class SportingEventBySourceIdSpec(string sourceKey, string externalId) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q) =>
        q.Where(e => e.SourceKey == sourceKey && e.ExternalId == externalId);
}

/// <summary>Everything still to come from one source, for reconciling against a fresh scrape.</summary>
public class FutureEventsBySourceSpec(string sourceKey, DateTime fromUtc) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q) =>
        q.Where(e => e.SourceKey == sourceKey && e.StartUtc >= fromUtc);
}

/// <summary>Events whose detail has not been collected yet.</summary>
public class EventsNeedingDetailsSpec(DateTime fromUtc) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q) =>
        q.Where(e => e.DetailsFetchedAtUtc == null && e.StartUtc >= fromUtc)
         .OrderBy(e => e.StartUtc);
}

/// <summary>
/// Events due in the given window that have no title yet. Detail must already be collected, so a
/// title is never produced with a missing cast or venue.
/// </summary>
public class EventsNeedingTitlesSpec(DateTime fromUtc, DateTime toUtc) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q) =>
        q.Where(e => e.TitleId == null
                     && e.DetailsFetchedAtUtc != null
                     && e.StartUtc >= fromUtc
                     && e.StartUtc < toUtc)
         .OrderBy(e => e.StartUtc);
}

/// <summary>
/// Still-to-come events whose detail is worth collecting again, because something it produced was
/// thrown away. Scoped to a league when only that league changed.
/// </summary>
public class FutureEventsForRecollectSpec(DateTime fromUtc, int? leagueId) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q) =>
        q.Where(e => e.StartUtc >= fromUtc
                     && e.DetailsFetchedAtUtc != null
                     && (leagueId == null || e.LeagueId == leagueId));
}

public class EventsByTitleIdsSpec(IReadOnlyCollection<Guid> titleIds) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q) =>
        q.Where(e => e.TitleId != null && titleIds.Contains(e.TitleId.Value));
}

public class ExpiredEventsSpec(DateTime beforeUtc) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q) =>
        q.Where(e => e.EndUtc < beforeUtc);
}

public class SportingEventsInRangeSpec(DateTime? fromUtc, DateTime? toUtc) : ISpecification<SportingEvent>
{
    public IQueryable<SportingEvent> Apply(IQueryable<SportingEvent> q)
    {
        if (fromUtc.HasValue) q = q.Where(e => e.StartUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(e => e.StartUtc < toUtc.Value);
        return q.OrderBy(e => e.StartUtc);
    }
}
