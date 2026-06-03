using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface ILinkResolver
{
    StreamingService Service { get; }
    Task<string?> ResolveAsync(string? rawLink, CancellationToken ct);
}
