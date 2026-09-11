using Hangfire;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Nfo;

internal static class GoLiveScheduler
{
    public static void Reschedule(IBackgroundJobClient jobClient, Title title)
    {
        if (title.LiveJobId is not null)
        {
            jobClient.Delete(title.LiveJobId);
            title.SetLiveJobId(null);
        }

        if (!title.Production.GoesLive || title.IsLive || title.StartUtc is not { } startUtc)
            return;

        var scheduledStartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);

        if (scheduledStartUtc <= DateTime.UtcNow)
            return;

        var titleId = title.Id;
        title.SetLiveJobId(jobClient.Schedule<TitleGoesLiveService>(
            s => s.MarkAsLiveAsync(titleId, scheduledStartUtc, CancellationToken.None),
            new DateTimeOffset(scheduledStartUtc)));
    }
}
