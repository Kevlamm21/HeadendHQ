using Hangfire;
using HeadendHQ.Core;

namespace HeadendHQ.VodLauncher;

public static class TitleProductionJobs
{
    public static void EnqueueAdbMapping(IBackgroundJobClient jobClient, Guid titleId) =>
        jobClient.Enqueue<AdbMappingService>(s => s.MapSingleAsync(titleId, CancellationToken.None));

    public static void EnqueueProduction(IBackgroundJobClient jobClient, Guid titleId) =>
        jobClient.Enqueue<ICreationService>(s => s.CreateForTitleAsync(titleId, CancellationToken.None));
}
