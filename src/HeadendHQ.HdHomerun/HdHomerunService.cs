using System.Text.Json;
using HeadendHQ.Core;
using HeadendHQ.Core.Options;
using HeadendHQ.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.HdHomerun;

public class HdHomerunService(
    HttpClient httpClient,
    AppDbContext dbContext,
    IConfiguration configuration,
    ILogger<HdHomerunService> logger) : IHdHomerunService
{
    public async Task RefreshXmltvAsync(CancellationToken cancellationToken = default)
    {
        var deviceUrl = configuration["HdHomerun:DeviceUrl"];

        if (string.IsNullOrWhiteSpace(deviceUrl))
        {
            logger.LogError("HdHomerun:DeviceUrl is not configured. Set the HdHomerun__DeviceUrl environment variable.");
            return;
        }

        logger.LogInformation("Fetching HDHomeRun device info from {Url}", deviceUrl);

        var discoverJson = await httpClient.GetStringAsync(deviceUrl, cancellationToken);
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

        var device = await dbContext.HdHomerunDevices.FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (device is null)
        {
            device = new HdHomerunDeviceOptions();
            dbContext.HdHomerunDevices.Add(device);
        }

        device.DeviceAuth = deviceAuth;
        device.XmltvUrl = xmltvUrl;
        device.XmltvContent = xmltvContent;
        device.LastUpdated = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("HDHomeRun XMLTV content updated successfully at {Time}", device.LastUpdated);
    }

    public async Task<string?> GetXmltvContentAsync(CancellationToken cancellationToken = default)
    {
        var device = await dbContext.HdHomerunDevices.FirstOrDefaultAsync(cancellationToken: cancellationToken);
        return device?.XmltvContent;
    }
}
