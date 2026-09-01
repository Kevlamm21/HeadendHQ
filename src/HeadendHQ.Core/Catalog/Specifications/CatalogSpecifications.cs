using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Specifications;

public class SportBySlugSpec(string slug) : ISpecification<Sport>
{
    public IQueryable<Sport> Apply(IQueryable<Sport> queryable) => queryable.Where(s => s.Slug == slug);
}

public class SportByExternalIdSpec(string sourceKey, string externalId) : ISpecification<Sport>
{
    public IQueryable<Sport> Apply(IQueryable<Sport> queryable) =>
        queryable.Where(s => s.ExternalRefs.Any(r => r.SourceKey == sourceKey && r.ExternalId == externalId));
}

public class LeagueBySlugSpec(string slug) : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) => queryable.Where(l => l.Slug == slug);
}

public class LeagueByExternalIdSpec(string sourceKey, string externalId) : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) =>
        queryable.Where(l => l.ExternalRefs.Any(r => r.SourceKey == sourceKey && r.ExternalId == externalId));
}

public class LeaguesBySportSpec(int sportId) : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) => queryable.Where(l => l.SportId == sportId);
}

public class FollowedLeaguesSpec : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) => queryable.Where(l => l.IsFollowed);
}

public class TeamsByLeagueSpec(int leagueId) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) =>
        queryable.Where(t => t.LeagueId == leagueId).OrderBy(t => t.DisplayName);
}

public class TeamByExternalIdSpec(int leagueId, string sourceKey, string externalId) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) =>
        queryable.Where(t => t.LeagueId == leagueId &&
            t.ExternalRefs.Any(r => r.SourceKey == sourceKey && r.ExternalId == externalId));
}

public class TeamByLeagueAndNameSpec(int leagueId, string displayName) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) =>
        queryable.Where(t => t.LeagueId == leagueId && t.DisplayName == displayName);
}

public class FollowedTeamsSpec : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) => queryable.Where(t => t.IsFollowed);
}

public class TeamsByIdsSpec(IReadOnlyCollection<int> ids) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) => queryable.Where(t => ids.Contains(t.Id));
}

public class BroadcasterBySlugSpec(string slug) : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) =>
        queryable.Where(b => b.Slug == slug || b.Aliases.Contains(slug));
}

/// <summary>
/// Broadcasters whose source record has never been read, so their logo is still unresolved.
/// </summary>
public class BroadcastersNeedingDetailSpec : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) =>
        queryable.Where(b => b.DetailFetchedAtUtc == null);
}

public class BroadcasterByExternalIdSpec(string sourceKey, string externalId) : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) =>
        queryable.Where(b => b.ExternalRefs.Any(r => r.SourceKey == sourceKey && r.ExternalId == externalId));
}

public class SubscribedBroadcastersSpec : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) => queryable.Where(b => b.IsSubscribed);
}

public class AthleteByExternalIdSpec(string sourceKey, string externalId) : ISpecification<Athlete>
{
    public IQueryable<Athlete> Apply(IQueryable<Athlete> queryable) =>
        queryable.Where(a => a.ExternalRefs.Any(r => r.SourceKey == sourceKey && r.ExternalId == externalId));
}

public class AthletesByIdsSpec(IReadOnlyCollection<int> ids) : ISpecification<Athlete>
{
    public IQueryable<Athlete> Apply(IQueryable<Athlete> queryable) => queryable.Where(a => ids.Contains(a.Id));
}

/// <summary>
/// Every athlete's identity without their headshot slot, for building a scrape-time lookup.
/// Projected so a scrape does not pull thousands of image rows it will not use.
/// </summary>
public record AthleteRef(int Id, string SourceKey, string ExternalId, bool HasHeadshot);

public class AthleteRefsSpec(string sourceKey) : ISpecification<Athlete, AthleteRef>
{
    public IQueryable<AthleteRef> Apply(IQueryable<Athlete> queryable) =>
        queryable
            .SelectMany(
                a => a.ExternalRefs.Where(r => r.SourceKey == sourceKey),
                (a, r) => new AthleteRef(a.Id, r.SourceKey, r.ExternalId, a.Headshot.ImageId != null));
}

public class CatalogSyncStateSpec : ISpecification<CatalogSyncState>
{
    public IQueryable<CatalogSyncState> Apply(IQueryable<CatalogSyncState> queryable) => queryable;
}

public class AthletesByTeamSpec(int teamId) : ISpecification<Athlete>
{
    public IQueryable<Athlete> Apply(IQueryable<Athlete> queryable) => queryable.Where(a => a.TeamId == teamId);
}

