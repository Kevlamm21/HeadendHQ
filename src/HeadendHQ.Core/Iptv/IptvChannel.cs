using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Iptv;

/// <summary>
/// One tunable channel from the IPTV lineup: what to tune (<see cref="GuideNumber"/>) and how to
/// recognise it (<see cref="CallSign"/>). The lineup is the only reliable signal for which local
/// affiliates this house can actually watch, so a broadcaster is auto-tuned only when its call sign
/// matches a row here.
/// </summary>
public class IptvChannel : IEntity<int>
{
    private IptvChannel() { }

    public IptvChannel(string guideNumber, string? callSign, string? displayName, string? iconUrl)
    {
        GuideNumber = guideNumber;
        Update(callSign, displayName, iconUrl);
    }

    public int Id { get; init; }

    /// <summary>The tuner's channel number, e.g. "19.1".</summary>
    public string GuideNumber { get; private set; } = string.Empty;

    /// <summary>Normalised call sign, e.g. "WXIX", or <c>null</c> when the lineup entry has none.</summary>
    public string? CallSign { get; private set; }

    public string? DisplayName { get; private set; }
    public string? IconUrl { get; private set; }

    /// <summary>
    /// Last refresh that still listed this channel. A row is never deleted when it drops out of the
    /// lineup — it may be a hand-set mapping target — so this is how staleness is judged.
    /// </summary>
    public DateTimeOffset LastSeenUtc { get; private set; }

    public void Update(string? callSign, string? displayName, string? iconUrl)
    {
        CallSign = callSign;
        DisplayName = displayName;
        IconUrl = iconUrl;
        LastSeenUtc = DateTimeOffset.UtcNow;
    }
}
