namespace HeadendHQ.Core;

public interface IBrowserContextFactory
{
    Task<IBrowserContext> CreateAsync(BrowserContextOptions? options = null, CancellationToken ct = default);
}

public class BrowserContextOptions
{
    public string? UserAgent { get; init; }
}
