using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.HdHomerun;

public static class HdHomerunExtensions
{
    public static void ConfigureHdHomerun(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IHdHomerunService, HdHomerunService>(client =>
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; HeadendHQ/1.0)"));
    }
}
