using System.Text.Json;
using HeadendHQ.Core;
using HeadendHQ.Core.HdHomerun;
using HeadendHQ.Core.Shared;
using HeadendHQ.HdHomerun.Settings;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.HdHomerun;

public class HdHomerunService(
    HttpClient httpClient,
    IWorkspace workspace,
    IMediator mediator,
    ILogger<HdHomerunService> logger) : IHdHomerunService
{
    public async Task RefreshXmltvAsync(CancellationToken cancellationToken = default)
    {
        var config = await mediator.Send(new GetHdHomerunSettingsQuery(), cancellationToken);

        if (string.IsNullOrWhiteSpace(config.DeviceUrl))
        {
            logger.LogError("HdHomerun DeviceUrl is not configured. Update it via the settings endpoint.");
            return;
        }

        logger.LogInformation("Fetching HDHomeRun device info from {Url}", config.DeviceUrl);

        var discoverJson = await httpClient.GetStringAsync(config.DeviceUrl, cancellationToken);
        using var doc = JsonDocument.Parse(discoverJson);

        if (!doc.RootElement.TryGetProperty("DeviceAuth", out var deviceAuthElement))
        {
            logger.LogError("DeviceAuth field not found in discover.json response");
            return;
        }

        var deviceAuth = deviceAuthElement.GetString()!;
        var xmltvUrl = $"https://api.hdhomerun.com/api/xmltv?DeviceAuth={deviceAuth}";

        logger.LogInformation("Fetching XMLTV content from HDHomeRun API");

        var xmltvContent = await httpClient.GetStringAsync(xmltvUrl, cancellationToken);

        var existing = await workspace.LoadSingleOrDefault(AllSpecification<XmltvCache>.Instance, cancellationToken);
        if (existing is not null)
            workspace.Remove(existing);

        workspace.Add(new XmltvCache { XmltvContent = xmltvContent });

        logger.LogInformation("HDHomeRun XMLTV content updated successfully.");
    }

    public async Task<string?> GetXmltvContentAsync(CancellationToken cancellationToken = default)
    {
        var cache = await workspace.LoadSingleOrDefault(AllSpecification<XmltvCache>.Instance, cancellationToken);
        return cache?.XmltvContent;
    }
}
