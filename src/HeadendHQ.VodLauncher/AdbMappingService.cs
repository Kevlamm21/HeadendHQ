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
    private readonly Dictionary<StreamingService, IAdbExtractor> _extractorMap =
        extractors.ToDictionary(e => e.Service);

    public async Task MapSingleAsync(Guid titleId, CancellationToken ct = default)
    {
        var title = await workspace.LoadById<Title, Guid>(titleId, ct);

        if (!_extractorMap.TryGetValue(title.StreamingService, out var extractor))
        {
            logger.LogWarning("No ADB extractor for {Service}. Skipping title {Id} ({Name}).", title.StreamingService, title.Id, title.Name);
            return;
        }

        title.AdbCommand = await extractor.BuildCommandAsync(title.EventUrl, ct);
        logger.LogInformation("ADB mapped title {Id} ({Name}).", title.Id, title.Name);
    }
}
