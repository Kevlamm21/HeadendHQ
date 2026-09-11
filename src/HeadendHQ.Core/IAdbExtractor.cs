namespace HeadendHQ.Core;

public interface IAdbExtractor
{
    string BroadcasterSlug { get; }
    Task<string?> BuildCommandAsync(string? eventUrl, CancellationToken ct);
}
