using HeadendHQ.Core.Catalog.Leagues.Specifications;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues;

public static class LeagueCatalog
{
    public static async Task<int> UpsertAsync(
        IWorkspace workspace, IMediator mediator, Sport sport, string sourceKey,
        IReadOnlyList<LeagueDescriptor> descriptors, CancellationToken ct)
    {
        var logos = 0;

        foreach (var descriptor in descriptors)
        {
            var league = await workspace.LoadSingleOrDefault(new LeagueBySlugSpec(descriptor.Slug), ct);

            if (league is null)
            {
                league = new League(sport.Id, descriptor.Slug, descriptor.Name);
                workspace.Add(league);
            }

            league.Describe(descriptor.Name, descriptor.Abbreviation, descriptor.ShortName, descriptor.SupportsTeams);
            league.TrackSource(sourceKey, descriptor.ExternalId);

            if (!league.HasFetchedLogos && await StoreLogosAsync(mediator, league, descriptor.Logos, false, ct) > 0)
                logos++;
        }

        return logos;
    }

    public static async Task<int> StoreLogosAsync(
        IMediator mediator, League league, IReadOnlyList<ImageCandidate>? candidates, bool revalidate,
        CancellationToken ct)
    {
        var download = await CatalogLogoDownloader.DownloadAsync(
            mediator, LogoPolicy.League, candidates, ImagePurpose.LeagueLogo, revalidate, ct);

        league.StoreFetchedLogos(download);
        return download.Stored.Count;
    }
}
