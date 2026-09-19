using HeadendHQ.Core;

namespace HeadendHQ.WebScraping.AmazonPrime;

internal sealed class AmazonPrimeLinkResolver : ILinkResolver
{
    public Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        return Task.FromResult(rawLink);
    }
}
