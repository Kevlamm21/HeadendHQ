using HeadendHQ.Core;
using HeadendHQ.WebScraping.Espn.Catalog;
using HeadendHQ.WebScraping.Espn.Transport;
using HeadendHQ.WebScraping.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.WebScraping.Espn;

internal static class EspnExtensions
{
    internal static void ConfigureEspn(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransportProfiles(EspnProfiles.All);

        // Registered once and handed out under both interfaces, so a scope gets one instance rather
        // than two of the same class.
        builder.Services.AddScoped<EspnCatalogSource>();
        builder.Services.AddScoped<ISportsCatalogSource>(sp => sp.GetRequiredService<EspnCatalogSource>());
        builder.Services.AddScoped<IBroadcasterCatalogSource>(sp => sp.GetRequiredService<EspnCatalogSource>());
        builder.Services.AddScoped<IScheduleSource, EspnScheduleSource>();
        builder.Services.AddScoped<IEventDetailSource, EspnEventDetailSource>();

        builder.Services.AddSingleton<EspnLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, EspnExtractor>();
    }
}
