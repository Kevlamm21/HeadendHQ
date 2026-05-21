namespace HeadendHQ.Core.SportingEvents;

public interface IAdbMappingService
{
    Task MapPendingAsync(CancellationToken ct = default);
}
