using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles.Specifications;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Titles;

public class AdbMappingService(
    IEnumerable<IAdbExtractor> extractors,
    IWorkspace workspace,
    ILogger<AdbMappingService> logger)
{
    private readonly Dictionary<StreamingService, IAdbExtractor> _extractorMap =
        extractors.ToDictionary(e => e.Service);

    public async Task MapPendingAsync(CancellationToken ct = default)
    {
        var pending = await workspace.Load(new PendingAdbMappingSpec(), ct);

        if (pending.Count == 0)
        {
            logger.LogInformation("ADB mapping: no pending titles.");
            return;
        }

        var mapped = 0;
        var skipped = 0;

        foreach (var title in pending)
        {
            if (!_extractorMap.TryGetValue(title.StreamingService, out var extractor))
            {
                logger.LogWarning(
                    "No ADB extractor registered for {Service}. Skipping title {Id} ({Name}).",
                    title.StreamingService, title.Id, title.Name);
                skipped++;
                continue;
            }

            try
            {
                title.AdbCommand = await extractor.BuildCommandAsync(title.EventUrl, ct);
                mapped++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to build ADB command for title {Id} ({Name}) on {Service}.",
                    title.Id, title.Name, title.StreamingService);
                skipped++;
            }
        }

        logger.LogInformation(
            "ADB mapping complete. Mapped: {Mapped}, Skipped: {Skipped}.",
            mapped, skipped);
    }
}
