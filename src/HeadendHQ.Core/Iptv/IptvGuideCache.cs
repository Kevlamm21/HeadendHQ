namespace HeadendHQ.Core.Iptv;

/// <summary>
/// The cached programme-guide document (XMLTV). One row, replaced wholesale on every refresh and
/// served at a stable URL for downstream consumers (ErsatzTV, Jellyfin).
/// </summary>
public record IptvGuideCache
{
    public int Id { get; init; }
    public string? Content { get; init; }
}
