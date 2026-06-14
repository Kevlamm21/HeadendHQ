using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.ScheduleScraping;

public static class ScheduleScrapingExtensions
{
    public static void ConfigureScheduleScraping(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<StreamingServiceMapper>();
    }
}
