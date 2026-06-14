using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Nfo;

public static class NfoExtensions
{
    public static void ConfigureNfo(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<NfoWriter>();
        builder.Services.AddScoped<INfoWriter>(sp => sp.GetRequiredService<NfoWriter>());
    }
}
