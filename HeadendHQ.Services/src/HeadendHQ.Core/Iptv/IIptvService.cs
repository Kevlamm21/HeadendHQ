namespace HeadendHQ.Core.Iptv;

public interface IIptvService
{
    Task RefreshGuideAsync(CancellationToken cancellationToken = default);

    Task<string?> GetGuideContentAsync(CancellationToken cancellationToken = default);

    Task RefreshLineupAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IptvChannel>> GetLineupAsync(CancellationToken cancellationToken = default);
}
