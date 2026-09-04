using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

/// <summary>
/// Debugging aid: removes a league's team logos and any image blob that no longer has a reference
/// anywhere. The teams themselves are kept, so the next refresh re-downloads their marks.
/// </summary>
public record DeleteAllLeagueTeamImagesCommand(int LeagueId) : ICommand<int>;

public class DeleteAllLeagueTeamImagesHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllLeagueTeamImagesCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllLeagueTeamImagesCommand command, CancellationToken ct)
    {
        var teams = await workspace.Load(new TeamsByLeagueSpec(command.LeagueId), ct);
        var candidates = teams.SelectMany(team => team.Logos).Select(logo => logo.ImageId).ToHashSet();

        foreach (var team in teams)
            team.ClearLogos();

        // Built after the rows are gone, so a blob kept alive only by the logos just removed is
        // correctly seen as unreferenced.
        var referenced = new HashSet<int>();

        foreach (var team in await workspace.LoadAll<Team>(ct))
            foreach (var logo in team.Logos)
                referenced.Add(logo.ImageId);

        foreach (var league in await workspace.LoadAll<League>(ct))
        {
            foreach (var logo in league.Logos)
                referenced.Add(logo.ImageId);

            foreach (var wordmark in league.Wordmarks)
                referenced.Add(wordmark.ImageId);
        }

        foreach (var broadcaster in await workspace.LoadAll<Broadcaster>(ct))
            foreach (var logo in broadcaster.Logos)
                referenced.Add(logo.ImageId);

        foreach (var title in await workspace.LoadAll<Title>(ct))
        {
            AddImageId(title.Artwork.PrimaryLogoImageId, referenced);
            AddImageId(title.Artwork.SecondaryLogoImageId, referenced);
            AddImageId(title.Artwork.BadgeImageId, referenced);
            AddImageId(title.Artwork.ProviderLogoImageId, referenced);
            AddImageId(title.Artwork.WordmarkImageId, referenced);

            foreach (var castMember in title.Cast)
                AddImageId(castMember.HeadshotImageId, referenced);
        }

        var deleted = 0;
        foreach (var image in await workspace.LoadAll<Image>(ct))
        {
            if (candidates.Contains(image.Id) && !referenced.Contains(image.Id))
            {
                workspace.Remove(image);
                deleted++;
            }
        }

        return deleted;
    }

    private static void AddImageId(int? imageId, HashSet<int> imageIds)
    {
        if (imageId is { } id)
            imageIds.Add(id);
    }
}
