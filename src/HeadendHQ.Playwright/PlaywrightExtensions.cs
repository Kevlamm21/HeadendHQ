using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Playwright;

public static class PlaywrightExtensions
{
    public static void ConfigurePlaywright(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<BrowserSessionManager>();
        builder.Services.AddSingleton<IBrowserSessionProvider>(sp => sp.GetRequiredService<BrowserSessionManager>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<BrowserSessionManager>());
    }
}
