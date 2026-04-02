using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("GetHealth")
    .WithSummary("Health check")
    .WithDescription("Returns the current health status of the API.");

app.Run();
