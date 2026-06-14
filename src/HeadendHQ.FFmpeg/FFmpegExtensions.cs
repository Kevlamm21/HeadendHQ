using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.FFmpeg;

public static class FFmpegExtensions
{
    public static void ConfigureFFmpeg(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IVideoCreator, FfmpegVideoCreator>();
    }
}
