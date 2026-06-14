using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HeadendHQ.AspNet;

public static class ErrorHandlingExtensions
{
    public static ProblemDetailsOptions MapExceptionToStatusCode<T>(
        this ProblemDetailsOptions options,
        int statusCode,
        string? title = null,
        string? type = null,
        Func<T, string>? detail = null) where T : Exception
    {
        return options.MapException<T>((problemDetails, exception) =>
        {
            Defaults.TryGetValue(statusCode, out var typeTitlePair);
            problemDetails.Status = statusCode;
            problemDetails.Type = type ?? typeTitlePair.Type;
            problemDetails.Title = title ?? typeTitlePair.Title;
            problemDetails.Detail = detail is not null ? detail(exception) : exception.Message;
        });
    }

    public static ProblemDetailsOptions MapException<T>(this ProblemDetailsOptions options, Action<ProblemDetails, T> action) where T : Exception
    {
        var customization = options.CustomizeProblemDetails;

        options.CustomizeProblemDetails = ctx =>
        {
            customization?.Invoke(ctx);

            var exceptionFeature = ctx.HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error is not T exception)
                return;

            action.Invoke(ctx.ProblemDetails, exception);

            if (ctx.ProblemDetails.Status.HasValue && ctx.ProblemDetails.Status != ctx.HttpContext.Response.StatusCode)
                ctx.HttpContext.Response.StatusCode = ctx.ProblemDetails.Status.Value;
        };

        return options;
    }

    private static readonly Dictionary<int, (string Type, string Title)> Defaults = new()
    {
        [400] = ("https://tools.ietf.org/html/rfc9110#section-15.5.1", "Bad Request"),
        [404] = ("https://tools.ietf.org/html/rfc9110#section-15.5.5", "Not Found"),
        [500] = ("https://tools.ietf.org/html/rfc9110#section-15.6.1", "Internal Server Error"),
    };
}
