using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.SixLabors;

public static class SixLaborsExtensions
{
    public static void ConfigureSixLabors(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IImageCreationService, ImageCreationService>();
        builder.Services.AddSingleton<IImageNormalizer, ImageNormalizer>();
    }
}
