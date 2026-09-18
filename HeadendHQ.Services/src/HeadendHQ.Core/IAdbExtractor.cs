namespace HeadendHQ.Core;

public interface IAdbExtractor
{
    string ProviderKey { get; }
    string Name { get; }
    Task<string?> BuildCommandAsync(string? eventUrl, CancellationToken ct);
}
