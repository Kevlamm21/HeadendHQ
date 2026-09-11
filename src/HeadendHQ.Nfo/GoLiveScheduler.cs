using Hangfire;
using HeadendHQ.Core.Shared;
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

        if (!title.Production.GoesLive || title.StartUtc is not { } startUtc)
            return;

        if (!LocalDay.IsToday(startUtc))
            return;

        var scheduledAt = new DateTimeOffset(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc));

        if (scheduledAt <= DateTimeOffset.UtcNow)
            return;

        var titleId = title.Id;
        title.SetLiveJobId(jobClient.Schedule<TitleGoesLiveService>(
            s => s.MarkAsLiveAsync(titleId, CancellationToken.None), scheduledAt));
    }
}
