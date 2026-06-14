using HeadendHQ.Web.CronJobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Web.Infrastructure;

public static class WebExtensions
{
    public static void ConfigureWeb(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<NightlyCronJob>();
    }
}
