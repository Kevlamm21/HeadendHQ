namespace HeadendHQ.Core.Iptv;

public record IptvGuideCache
{
    public int Id { get; init; }
    public string? Content { get; init; }
}
