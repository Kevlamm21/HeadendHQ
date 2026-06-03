using HeadendHQ.Core;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.AmazonPrime;

public class AmazonPrimeLinkResolver : ILinkResolver
{
    public StreamingService Service => StreamingService.AmazonPrime;

    public Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        return Task.FromResult(rawLink);
    }
}
