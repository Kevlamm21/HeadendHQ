namespace HeadendHQ.Core;

public interface ILogoNormalizer
{
    Task<byte[]> NormalizeTeamLogoAsync(byte[] input, CancellationToken ct = default);
    Task<byte[]> NormalizeLeagueLogoAsync(byte[] input, CancellationToken ct = default);
    Task<byte[]> NormalizeStreamingLogoAsync(byte[] input, CancellationToken ct = default);
    Task<byte[]> NormalizeWordMarkAsync(byte[] input, CancellationToken ct = default);
}
