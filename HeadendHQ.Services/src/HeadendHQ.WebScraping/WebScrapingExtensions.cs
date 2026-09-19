using HeadendHQ.Core;
using HeadendHQ.WebScraping.AmazonPrime;
using HeadendHQ.WebScraping.Espn;
using HeadendHQ.WebScraping.Nba;
using HeadendHQ.WebScraping.Peacock;
using HeadendHQ.WebScraping.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.WebScraping;

public static class WebScrapingExtensions
{
    public static void ConfigureWebScraping(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<WebTransport>(c => c.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All,
            });

        builder.Services.AddSingleton<TransportRegistry>();
        builder.Services.AddScoped<IImageFetcher, WebImageFetcher>();

        builder.ConfigureEspn();
        builder.ConfigurePeacock();
        builder.ConfigureAmazonPrime();
        builder.ConfigureNba();
    }
}
