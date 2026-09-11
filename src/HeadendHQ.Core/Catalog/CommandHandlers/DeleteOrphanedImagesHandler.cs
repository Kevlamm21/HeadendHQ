using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Events;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Catalog.CommandHandlers;

public record DeleteOrphanedImagesCommand(IReadOnlyCollection<int> CandidateImageIds) : ICommand<int>;

public class DeleteOrphanedImagesHandler(IWorkspace workspace, IReadModel readModel)
    : ICommandHandler<DeleteOrphanedImagesCommand, int>
{
    public async ValueTask<int> Handle(DeleteOrphanedImagesCommand command, CancellationToken ct)
    {
        var candidates = command.CandidateImageIds.Distinct().ToList();

        if (candidates.Count == 0)
            return 0;

        var referenced = new HashSet<int>();

        referenced.UnionWith(await readModel.Search(new TeamLogoImageIdsSpec(candidates), ct));
        referenced.UnionWith(await readModel.Search(new LeagueLogoImageIdsSpec(candidates), ct));
        referenced.UnionWith(await readModel.Search(new BroadcasterLogoImageIdsSpec(candidates), ct));
        referenced.UnionWith(await readModel.Search(new EventHeadshotImageIdsSpec(candidates), ct));

        foreach (var title in await readModel.All<Title>(ct))
        {
            AddImageId(title.PosterImageId, referenced);
            AddImageId(title.BackgroundImageId, referenced);
            AddImageId(title.ThumbnailImageId, referenced);
            AddImageId(title.ClearLogoImageId, referenced);

            foreach (var castMember in title.Cast)
                AddImageId(castMember.HeadshotImageId, referenced);
        }

        var deleted = 0;

        foreach (var id in candidates.Where(id => !referenced.Contains(id)))
        {
            if (await workspace.LoadSingleOrDefault(new ImageByIdSpec(id), ct) is not { } image)
                continue;

            workspace.Remove(image);
            deleted++;
        }

        return deleted;
    }

    private static void AddImageId(int? imageId, HashSet<int> imageIds)
    {
        if (imageId is { } id)
            imageIds.Add(id);
    }

    private class TeamLogoImageIdsSpec(List<int> ids) : ISpecification<Team, int>
    {
        public IQueryable<int> Apply(IQueryable<Team> queryable) =>
            queryable.SelectMany(t => t.Logos).Select(l => l.ImageId).Where(id => ids.Contains(id));
    }

    private class LeagueLogoImageIdsSpec(List<int> ids) : ISpecification<League, int>
    {
        public IQueryable<int> Apply(IQueryable<League> queryable) =>
            queryable.SelectMany(l => l.Logos).Select(l => l.ImageId)
                .Concat(queryable.SelectMany(l => l.Wordmarks).Select(w => w.ImageId))
                .Where(id => ids.Contains(id));
    }

    private class BroadcasterLogoImageIdsSpec(List<int> ids) : ISpecification<Broadcaster, int>
    {
        public IQueryable<int> Apply(IQueryable<Broadcaster> queryable) =>
            queryable.SelectMany(b => b.Logos).Select(l => l.ImageId).Where(id => ids.Contains(id));
    }

    private class EventHeadshotImageIdsSpec(List<int> ids) : ISpecification<SportingEvent, int>
    {
        public IQueryable<int> Apply(IQueryable<SportingEvent> queryable) =>
            queryable.SelectMany(e => e.Cast)
                .Where(c => c.HeadshotImageId != null && ids.Contains(c.HeadshotImageId.Value))
                .Select(c => c.HeadshotImageId!.Value);
    }
}
