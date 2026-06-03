using HeadendHQ.Core.Browser;

namespace HeadendHQ.Peacock;

public class PeacockLinkResolver(IBrowserContextFactory browserFactory)
{
    public async Task<string> ResolveAsync(string shortLink, CancellationToken ct)
    {
        await using var context = await browserFactory.CreateAsync(ct: ct);
        await using var page = await context.NewPageAsync();
        await page.GotoAsync(shortLink, BrowserWaitUntil.DomContentLoaded);

        ct.ThrowIfCancellationRequested();

        var uri = new Uri(page.Url);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}
