using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles.Specifications;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.VodLauncher;

public class VodCleanupService(
    IWorkspace workspace,
    ILogger<VodCleanupService> logger) : ICleanupService
{
    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        var expired = await workspace.Load(new ExpiredVideoTitlesSpec(DateTime.Now), ct);

        if (expired.Count == 0)
        {
            logger.LogInformation("VOD cleanup: no expired videos to remove.");
            return;
        }

        var deleted = 0;
        var failed = 0;

        foreach (var title in expired)
        {
            try
            {
                if (Directory.Exists(title.VodLauncherPath!))
                    Directory.Delete(title.VodLauncherPath!, recursive: true);

                title.VodLauncherPath = null;
                deleted++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to clean up VOD for title {Id} ({Name}).", title.Id, title.Name);
                failed++;
            }
        }

        logger.LogInformation("VOD cleanup complete. Deleted: {Deleted}, Failed: {Failed}.", deleted, failed);
    }
}
