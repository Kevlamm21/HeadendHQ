using HeadendHQ.WebScraping.AmazonPrime;
using HeadendHQ.WebScraping.Espn;
using HeadendHQ.WebScraping.Nba;
using HeadendHQ.WebScraping.Peacock;
using Microsoft.AspNetCore.Builder;

namespace HeadendHQ.WebScraping;

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
