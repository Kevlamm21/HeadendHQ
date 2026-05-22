namespace HeadendHQ.Core.SportingEvents;

public interface IVideoCreationService
{
    Task CreateDailyAsync(CancellationToken ct = default);
}
