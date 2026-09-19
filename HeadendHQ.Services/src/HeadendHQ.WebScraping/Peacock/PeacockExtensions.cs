using HeadendHQ.Core;
using HeadendHQ.WebScraping.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.WebScraping.Peacock;

internal static class PeacockExtensions
{
    internal static void ConfigurePeacock(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransportProfiles(PeacockProfiles.All);

        builder.Services.AddSingleton<PeacockLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, PeacockExtractor>();
    }
}
