#pragma warning disable ASPIREPERSISTENCE001

var builder = DistributedApplication.CreateBuilder(args);

var client = builder.AddJavaScriptApp("client", "../../../HeadendHQ.Client", "start")
    .WithHttpEndpoint(port: 4200, targetPort: 4200)
    .WithPersistentLifetime()
    .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);


var web = builder.AddProject<Projects.HeadendHQ_Web>("web")
    .WithEnvironment("ReverseProxy__Clusters__client__Destinations__dev__Address", client.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("https", url => url.DisplayText = "HeadendHQ")
    .WithUrlForEndpoint("https", _ => new ResourceUrlAnnotation { Url = "/scalar/v1", DisplayText = "API Docs" })
    .WithUrlForEndpoint("https", _ => new ResourceUrlAnnotation { Url = "/hangfire", DisplayText = "Hangfire Dashboard"});

builder.Build().Run();
