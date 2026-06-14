using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core;

public class StreamingServiceMapper(IMediator mediator)
{
    private GlobalSettings? _settings;

    public async Task<StreamingService?> MapAsync(string broadcasterDisplay, CancellationToken ct)
    {
        var service = Map(broadcasterDisplay);
        if (service is null) return null;

        _settings ??= await mediator.Send(new GetGlobalSettingsQuery(), ct);
        return _settings.EnabledStreamingServices.Contains(service.Value) ? service : null;
    }

    private static StreamingService? Map(string broadcasterDisplay)
    {
        var normalized = broadcasterDisplay.ToLowerInvariant().Replace(" ", "");
        return normalized switch
        {
            "espn" or "abc" => StreamingService.Espn,
            "nbatv" => StreamingService.NbaLeaguePass,
            "primevideo" or "amazon" or "amazonprime" => StreamingService.AmazonPrime,
            "peacock" or "nbc" => StreamingService.Peacock,
            _ => null
        };
    }
}
