using HeadendHQ.Core.SportingEvents;
using HeadendHQ.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.AdbMapping;

public class AdbMappingService(
    IEnumerable<IAdbExtractor> extractors,
    AppDbContext db,
    ILogger<AdbMappingService> logger) : IAdbMappingService
{
    private readonly Dictionary<StreamingService, IAdbExtractor> _extractorMap =
        extractors.ToDictionary(e => e.Service);

    public async Task MapPendingAsync(CancellationToken ct = default)
    {
        var pending = await db.SportingEvents
            .Where(e => e.AdbCommand == null)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            logger.LogInformation("ADB mapping: no pending events.");
            return;
        }

        var mapped = 0;
        var skipped = 0;

        foreach (var evt in pending)
        {
            if (!_extractorMap.TryGetValue(evt.StreamingService, out var extractor))
            {
                logger.LogWarning(
                    "No ADB extractor registered for {Service}. Skipping event {Id} ({Title}).",
                    evt.StreamingService, evt.Id, evt.Title);
                skipped++;
                continue;
            }

            try
            {
                evt.AdbCommand = extractor.BuildCommand(evt.EventUrl);
                mapped++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to build ADB command for event {Id} ({Title}) on {Service}.",
                    evt.Id, evt.Title, evt.StreamingService);
                skipped++;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "ADB mapping complete. Mapped: {Mapped}, Skipped: {Skipped}.",
            mapped, skipped);
    }
}
