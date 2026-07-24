using HeadendHQ.Core;
using HeadendHQ.Core.Titles;
using HeadendHQ.Playwright;

namespace HeadendHQ.Peacock;

public class PeacockLinkResolver(IBrowserSessionProvider sessionProvider) : ILinkResolver
{
    public StreamingService Service => StreamingService.Peacock;

    public async Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rawLink))
            return null;

        await using var session = await sessionProvider.CreateSessionAsync(ct);
        var page = await session.Context.NewPageAsync();

        await page.GotoAndWaitForDomContentAsync(rawLink, ct);

        var uri = new Uri(page.Url);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}
