using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Nba;

public static class NbaExtensions
{
    public static void ConfigureNba(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<NbaLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, NbaExtractor>();
    }
}
