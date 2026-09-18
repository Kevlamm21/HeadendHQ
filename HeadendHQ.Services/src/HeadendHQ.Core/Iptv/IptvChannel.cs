using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Iptv;

public class IptvChannel : IEntity<int>
{
    private IptvChannel() { }

    public IptvChannel(string guideNumber, string? callSign, string? displayName, string? iconUrl)
    {
        GuideNumber = guideNumber;
        Update(callSign, displayName, iconUrl);
    }

    public int Id { get; init; }

    public string GuideNumber { get; private set; } = string.Empty;

    public string? CallSign { get; private set; }

    public string? DisplayName { get; private set; }
    public string? IconUrl { get; private set; }

    public DateTimeOffset LastSeenUtc { get; private set; }

    public void Update(string? callSign, string? displayName, string? iconUrl)
    {
        CallSign = callSign;
        DisplayName = displayName;
        IconUrl = iconUrl;
        LastSeenUtc = DateTimeOffset.UtcNow;
    }
}
