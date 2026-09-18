using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.WebScraping.Peacock;

internal static class PeacockExtensions
{
    internal static void ConfigurePeacock(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<PeacockLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, PeacockExtractor>();
    }
}
