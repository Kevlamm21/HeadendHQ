using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using Mediator;

namespace HeadendHQ.Core.Catalog;

public record LogoDownload(IReadOnlyList<FetchedLogo> Stored, IReadOnlyCollection<string> Wanted)
{
    public static readonly LogoDownload Nothing = new([], []);

    public bool Complete => Stored.Count == Wanted.Count;
}

public static class CatalogLogoDownloader
{
    public static async Task<LogoDownload> DownloadAsync(
        IMediator mediator, LogoPolicy policy, IEnumerable<ImageCandidate>? candidates, ImagePurpose purpose,
        bool revalidate, CancellationToken ct, int? leagueId = null)
    {
        var chosen = policy.Choose(candidates);

        if (chosen.Count == 0)
            return LogoDownload.Nothing;

        var stored = new List<FetchedLogo>();

        foreach (var candidate in chosen)
        {
            if (await mediator.Send(
                    new MaterializeImageByUrlCommand(candidate.Url, purpose, leagueId, revalidate), ct)
                is { } imageId)
                stored.Add(new FetchedLogo(LogoPolicy.LabelFor(candidate), imageId));
        }

        return new LogoDownload(stored, [.. chosen.Select(LogoPolicy.LabelFor)]);
    }
}
