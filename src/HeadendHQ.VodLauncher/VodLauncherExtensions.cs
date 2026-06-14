using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.VodLauncher;

public static class VodLauncherExtensions
{
    public static void ConfigureVodLauncher(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<AdbMappingService>();
        builder.Services.AddScoped<ICreationService, VodCreationService>();
        builder.Services.AddScoped<ICleanupService, VodCleanupService>();
    }
}
