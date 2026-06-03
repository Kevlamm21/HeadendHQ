namespace HeadendHQ.Core;

public interface IBrowserContext : IAsyncDisposable
{
    Task<IBrowserPage> NewPageAsync();
    Task<string> ApiGetAsync(string url, IDictionary<string, string>? headers = null, CancellationToken ct = default);
}

