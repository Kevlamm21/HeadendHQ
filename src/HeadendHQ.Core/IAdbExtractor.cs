using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface IAdbExtractor
{
    StreamingService Service { get; }
    Task<string?> BuildCommandAsync(string? eventUrl, CancellationToken ct);
}
