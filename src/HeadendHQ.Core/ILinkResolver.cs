namespace HeadendHQ.Core;

public interface ILinkResolver
{
    Task<string?> ResolveAsync(string? rawLink, CancellationToken ct);
}
