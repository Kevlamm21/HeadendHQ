using HeadendHQ.Core.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

namespace HeadendHQ.AspNet;

public static class AspNetExtensions
{
    public static void ConfigureAspNet(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<JsonOptions>(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddOpenApi();
        builder.Services.AddMemoryCache();

        builder.Services.AddProblemDetails(options =>
        {
            options.MapExceptionToStatusCode<NotFoundException>(StatusCodes.Status404NotFound);
            options.MapExceptionToStatusCode<InvalidOperationException>(StatusCodes.Status400BadRequest);
            options.MapExceptionToStatusCode<ArgumentException>(StatusCodes.Status400BadRequest);
            options.MapExceptionToStatusCode<BadHttpRequestException>(
                StatusCodes.Status400BadRequest,
                detail: ex => ex.InnerException?.Message ?? ex.Message);
        });
    }

    public static void UseAspNet(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStaticFiles();
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "HeadendHQ | Api Documentation";
            options.Favicon = "/favicon.png";
        });
    }
}
