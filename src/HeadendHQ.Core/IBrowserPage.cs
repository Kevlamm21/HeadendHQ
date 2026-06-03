namespace HeadendHQ.Core;

public interface IBrowserPage : IAsyncDisposable
{
    string Url { get; }
    Task GotoAsync(string url, BrowserWaitUntil waitUntil = BrowserWaitUntil.Load);
    Task WaitForNetworkIdleAsync();
    Task<string> GetContentAsync();
}

public enum BrowserWaitUntil
{
    Load,
    DomContentLoaded,
    NetworkIdle
}