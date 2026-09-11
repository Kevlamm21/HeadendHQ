using HeadendHQ.Core;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.WebScraping.Espn.Catalog;
using HeadendHQ.WebScraping.Espn.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.WebScraping.Espn;

internal static class EspnExtensions
{
    internal static void ConfigureEspn(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<EspnTransport>(c => c.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All,
            });

        builder.Services.AddScoped<EspnApiGate>();
        builder.Services.AddScoped<EspnCdnGate>();

        builder.Services.AddScoped<ISportsCatalogSource, EspnSportsCatalogSource>();
        builder.Services.AddScoped<IBroadcasterCatalogSource, EspnBroadcasterCatalogSource>();
        builder.Services.AddScoped<IImageFetcher, EspnImageFetcher>();

        builder.Services.AddSingleton<EspnLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, EspnExtractor>();
        builder.Services.AddScoped<IScheduleSource, EspnScheduleSource>();
        builder.Services.AddScoped<IEventDetailSource, EspnEventDetailSource>();
    }
}
