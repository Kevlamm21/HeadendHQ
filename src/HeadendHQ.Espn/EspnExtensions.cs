using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Espn;

public static class EspnExtensions
{
    public static void ConfigureEspn(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<EspnLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, EspnExtractor>();
    }
}
