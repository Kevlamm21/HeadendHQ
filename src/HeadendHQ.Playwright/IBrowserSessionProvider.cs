using Microsoft.Playwright;

namespace HeadendHQ.Playwright;

public interface IBrowserSessionProvider
{
    Task<IBrowserSessionHandle> CreateSessionAsync(CancellationToken ct);
}

public interface IBrowserSessionHandle : IAsyncDisposable
{
    IBrowserContext Context { get; }
}
