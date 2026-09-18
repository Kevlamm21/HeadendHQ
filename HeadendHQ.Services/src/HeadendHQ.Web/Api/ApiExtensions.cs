namespace HeadendHQ.Web.Api;

public static class ApiExtensions
{
    public static void MapApi(this WebApplication app)
    {
        app.UseExceptionHandler();

        var api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithTags("Health")
            .WithName("GetHealth")
            .WithSummary("Health check")
            .WithDescription("Returns the current health status of the API.");

        api.MapIptvEndpoints();
        api.MapTitleEndpoints();
        api.MapEventEndpoints();
        api.MapCatalogEndpoints();
        api.MapStreamingEndpoints();
        api.MapMediaEndpoints();
        api.MapSettingsEndpoints();
    }
}
