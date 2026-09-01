namespace HeadendHQ.Core;

public interface ILinkResolver
{
    string BroadcasterSlug { get; }
    Task<string?> ResolveAsync(string? rawLink, CancellationToken ct);
}
