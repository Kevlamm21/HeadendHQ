using HeadendHQ.Core;

namespace HeadendHQ.WebScraping.AmazonPrime;

public class AmazonPrimeLinkResolver : ILinkResolver
{
    public string BroadcasterSlug => "prime-video";

    public Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        return Task.FromResult(rawLink);
    }
}
