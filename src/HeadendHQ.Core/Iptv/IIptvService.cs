namespace HeadendHQ.Core.Iptv;

/// <summary>
/// The seam over whatever supplies the electronic programme guide and the tunable channel lineup.
/// Implemented by <c>HeadendHQ.HdHomerun</c> today; the interface exists so Core and Web never name
/// a device vendor — the same reason ESPN sits behind <c>ISportsCatalogSource</c>.
/// </summary>
public interface IIptvService
{
    /// <summary>Re-fetch the programme-guide document and replace the cached copy.</summary>
    Task RefreshGuideAsync(CancellationToken cancellationToken = default);

    /// <summary>The cached programme guide (XMLTV), or <c>null</c> if none has been fetched yet.</summary>
    Task<string?> GetGuideContentAsync(CancellationToken cancellationToken = default);

    /// <summary>Re-read the tunable channel lineup and upsert <see cref="IptvChannel"/> rows.</summary>
    Task RefreshLineupAsync(CancellationToken cancellationToken = default);

    /// <summary>Every tunable channel currently on file, ordered by guide number.</summary>
    Task<IReadOnlyList<IptvChannel>> GetLineupAsync(CancellationToken cancellationToken = default);
}
