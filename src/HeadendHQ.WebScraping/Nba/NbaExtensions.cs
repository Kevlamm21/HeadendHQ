using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.WebScraping.Nba;

internal static class NbaExtensions
{
    internal static void ConfigureNba(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<NbaLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, NbaExtractor>();
    }
}
