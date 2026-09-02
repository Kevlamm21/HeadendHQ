using HeadendHQ.WebScraping.AmazonPrime;
using HeadendHQ.WebScraping.Espn;
using HeadendHQ.WebScraping.Nba;
using HeadendHQ.WebScraping.Peacock;
using Microsoft.AspNetCore.Builder;

namespace HeadendHQ.WebScraping;

/// <summary>
/// The single registration point for everything that reads a third party's site or API — ESPN's
/// catalog and schedule, and the per-service launch-link machinery.
/// <para>
/// One project rather than one per service: the work is the same shape everywhere (an HTTP or
/// Playwright request, a parse, a mapping into a Core descriptor), and a service is a folder here
/// rather than a csproj so adding one is not a solution change. Each service keeps its own internal
/// registration method, so what a service needs stays beside the service.
/// </para>
/// </summary>
public static class WebScrapingExtensions
{
    public static void ConfigureWebScraping(this WebApplicationBuilder builder)
    {
        builder.ConfigureEspn();
        builder.ConfigurePeacock();
        builder.ConfigureAmazonPrime();
        builder.ConfigureNba();
    }
}
