namespace HeadendHQ.Nba;

internal static class EspnLinkResolver
{
    public static async Task<string> ResolveAsync(HttpClient httpClient, string smartLink, CancellationToken ct)
    {
        var response = await httpClient.GetAsync(smartLink, HttpCompletionOption.ResponseHeadersRead, ct);
        var finalUrl = response.RequestMessage?.RequestUri?.ToString();
        return finalUrl ?? smartLink;
    }
}
