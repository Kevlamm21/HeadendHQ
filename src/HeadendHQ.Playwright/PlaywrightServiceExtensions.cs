using HeadendHQ.Core.Browser;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Playwright;

public static class PlaywrightServiceExtensions
{
    public static IServiceCollection AddPlaywright(this IServiceCollection services)
    {
        services.AddSingleton<PlaywrightBrowserContextFactory>();
        services.AddSingleton<IBrowserContextFactory>(sp => sp.GetRequiredService<PlaywrightBrowserContextFactory>());
        return services;
    }
}
