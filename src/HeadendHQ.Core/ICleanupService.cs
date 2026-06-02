namespace HeadendHQ.Core;

public interface ICleanupService
{
    Task CleanupExpiredAsync(CancellationToken ct = default);
}
