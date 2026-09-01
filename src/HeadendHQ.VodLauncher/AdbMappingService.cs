using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.Specifications;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.VodLauncher;

public class AdbMappingService(
    IEnumerable<IAdbExtractor> extractors,
    IWorkspace workspace,
    ILogger<AdbMappingService> logger)
{
    private readonly Dictionary<string, IAdbExtractor> _extractorMap =
        extractors.ToDictionary(e => e.BroadcasterSlug, StringComparer.OrdinalIgnoreCase);

    public async Task MapSingleAsync(Guid titleId, CancellationToken ct = default)
    {
        var title = await workspace.LoadSingleOrDefault(new EntityByIdSpecification<Title, Guid>(titleId), ct);

        if (title is null)
        {
            logger.LogWarning("Title {Id} no longer exists. Skipping ADB mapping.", titleId);
            return;
        }

        if (title.LaunchSlug is not { Length: > 0 } slug)
        {
            logger.LogWarning("Title {Id} ({Name}) has no launch slug. Skipping ADB mapping.", title.Id, title.Name);
            return;
        }

        if (!_extractorMap.TryGetValue(slug, out var extractor))
        {
            logger.LogWarning("No ADB extractor for {Slug}. Skipping title {Id} ({Name}).", slug, title.Id, title.Name);
            return;
        }

        title.SetAdbCommand(await extractor.BuildCommandAsync(title.EventUrl, ct));
        logger.LogInformation("ADB mapped title {Id} ({Name}).", title.Id, title.Name);
    }
}
