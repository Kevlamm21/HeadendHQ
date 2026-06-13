using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core;

public class StreamingServiceMapper(IMediator mediator)
{
    private GlobalSettings? _settings;

    public async Task<ResolvedBroadcaster?> FindEnabledAsync(IEnumerable<Broadcaster> broadcasters, CancellationToken ct)
    {
        _settings ??= await mediator.Send(new GetGlobalSettingsQuery(), ct);
        ResolvedBroadcaster? fallback = null;

        foreach (var broadcaster in broadcasters)
        {
            var service = Map(broadcaster.DisplayName);
            if (service is null || !_settings.EnabledStreamingServices.Contains(service.Value))
                continue;

            if (!string.IsNullOrEmpty(broadcaster.VideoLink))
                return new ResolvedBroadcaster { streamingService = service.Value, VideoLink = broadcaster.VideoLink };

            fallback ??= new ResolvedBroadcaster { streamingService = service.Value };
        }

        return fallback;
    }

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
