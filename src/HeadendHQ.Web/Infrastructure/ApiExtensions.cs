using HeadendHQ.Web.Api;

namespace HeadendHQ.Web.Infrastructure;

public static class ApiExtensions
{
    public static void MapApi(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect("/scalar/v1"))
            .ExcludeFromDescription();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithTags("Health")
            .WithName("GetHealth")
            .WithSummary("Health check")
            .WithDescription("Returns the current health status of the API.");

        app.MapHdHomerunEndpoints();
        app.MapTitleEndpoints();
        app.MapScheduleScraperEndpoints();
        app.MapAssetEndpoints();
        app.MapSettingsEndpoints();
    }
}
