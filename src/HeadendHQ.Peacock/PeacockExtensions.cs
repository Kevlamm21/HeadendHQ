using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Peacock;

public static class PeacockExtensions
{
    public static void ConfigurePeacock(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<PeacockLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, PeacockExtractor>();
    }
}
