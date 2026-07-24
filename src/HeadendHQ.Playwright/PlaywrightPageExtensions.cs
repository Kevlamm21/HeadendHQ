using Microsoft.Playwright;

namespace HeadendHQ.Playwright;

public static class PlaywrightPageExtensions
{
    public static async Task GotoAndWaitForLoadAsync(this IPage page, string url, CancellationToken ct)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        ct.ThrowIfCancellationRequested();
    }

    public static async Task GotoAndWaitForDomContentAsync(this IPage page, string url, CancellationToken ct)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        ct.ThrowIfCancellationRequested();
    }
}
