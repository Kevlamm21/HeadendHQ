using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Events;

/// <summary>
/// A sports <see cref="Title"/> and the <see cref="SportingEvent"/> it was produced from are one
/// lifecycle, not two — neither should outlive the other. This removes whichever set of events and
/// titles a caller has already paired up, then sweeps any artwork/headshot images nothing left in the
/// system still references. Callers are responsible for finding the pairing; this only removes exactly
/// what it's given.
/// </summary>
public static class TitleEventCleanup
{
    public static async Task<int> RemoveAsync(
        IWorkspace workspace,
        IReadOnlyCollection<SportingEvent> events,
        IReadOnlyCollection<Title> titles,
        CancellationToken ct)
    {
        var candidates = new HashSet<int>();

        foreach (var sportingEvent in events)
        {
            workspace.Remove(sportingEvent);
            foreach (var cast in sportingEvent.Cast)
                AddId(cast.HeadshotImageId, candidates);
        }

        foreach (var title in titles)
        {
            workspace.Remove(title);
            foreach (var id in ArtworkImageIds(title)) candidates.Add(id);
            foreach (var cast in title.Cast) AddId(cast.HeadshotImageId, candidates);
        }

        if (candidates.Count == 0)
            return 0;

        // Built after the rows above are gone, so a blob kept alive only by them is correctly seen
        // as unreferenced. No Origin guard needed — a real reference scan across every surviving
        // title and event already proves an image is safe to delete, Manual or Fetched alike.
        var removedEventIds = events.Select(e => e.Id).ToHashSet();
        var removedTitleIds = titles.Select(t => t.Id).ToHashSet();
        var referenced = new HashSet<int>();

        foreach (var sportingEvent in await workspace.LoadAll<SportingEvent>(ct))
        {
            if (removedEventIds.Contains(sportingEvent.Id)) continue;
            foreach (var cast in sportingEvent.Cast) AddId(cast.HeadshotImageId, referenced);
        }

        foreach (var title in await workspace.LoadAll<Title>(ct))
        {
            if (removedTitleIds.Contains(title.Id)) continue;
            foreach (var id in ArtworkImageIds(title)) referenced.Add(id);
            foreach (var cast in title.Cast) AddId(cast.HeadshotImageId, referenced);
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

    private static void AddId(int? id, HashSet<int> ids)
    {
        if (id is { } value)
            ids.Add(value);
    }

    private static IEnumerable<int> ArtworkImageIds(Title title) =>
        new[] { title.PosterImageId, title.BackgroundImageId, title.ThumbnailImageId, title.ClearLogoImageId }
            .Where(id => id is not null)
            .Select(id => id!.Value);
}
