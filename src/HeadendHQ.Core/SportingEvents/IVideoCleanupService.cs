namespace HeadendHQ.Core.SportingEvents;

public interface IVideoCleanupService
{
    Task CleanupExpiredAsync(CancellationToken ct = default);
}
