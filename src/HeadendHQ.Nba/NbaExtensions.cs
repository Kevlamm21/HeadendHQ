using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Nba;

public static class NbaExtensions
{
    public static void ConfigureNba(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<NbaScheduleScraper>();
        builder.Services.AddTransient<IScheduleScraper>(sp => sp.GetRequiredService<NbaScheduleScraper>());
        builder.Services.AddSingleton<NbaLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, NbaExtractor>();
    }
}
