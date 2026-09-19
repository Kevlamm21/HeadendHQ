using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Transport;

internal sealed class TransportRegistry : IDisposable
{
    private readonly IReadOnlyList<TransportProfile> _profiles;
    private readonly Dictionary<string, HostLimiter> _limiters;
    private readonly ConcurrentDictionary<string, TransportProfile> _byHost = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TransportRegistry> _logger;

    public TransportRegistry(IEnumerable<TransportProfile> profiles, ILoggerFactory loggers)
    {
        _logger = loggers.CreateLogger<TransportRegistry>();
        _profiles = [.. profiles];

        _limiters = _profiles
            .Append(TransportProfile.Default)
            .DistinctBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                p => p.Name,
                p => new HostLimiter(p, loggers.CreateLogger($"Transport.{p.Name}")),
                StringComparer.OrdinalIgnoreCase);
    }

    public TransportProfile For(string url)
    {
        var host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;

        return _byHost.GetOrAdd(host, h =>
        {
            var match = _profiles
                .Select(p => (Profile: p, Score: p.Match(h)))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Profile)
                .FirstOrDefault();

            if (match is not null)
                return match;

            _logger.LogInformation(
                "No transport profile claims {Host}; using the default limits.", h);

            return TransportProfile.Default;
        });
    }

    public HostLimiter LimiterFor(string url) => _limiters[For(url).Name];

    public Task<T> RunAsync<T>(string url, Func<CancellationToken, Task<T>> work, CancellationToken ct) =>
        LimiterFor(url).RunAsync(work, ct);

    public void Dispose()
    {
        foreach (var limiter in _limiters.Values)
            limiter.Dispose();
    }
}

internal static class TransportRegistration
{
    public static IServiceCollection AddTransportProfiles(
        this IServiceCollection services, IEnumerable<TransportProfile> profiles)
    {
        foreach (var profile in profiles)
            services.AddSingleton(profile);

        return services;
    }
}
