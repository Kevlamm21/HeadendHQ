using HeadendHQ.Core.Iptv;

namespace HeadendHQ.Core.Catalog.Broadcasters;

/// <summary>
/// Call sign → guide number, built once from the IPTV lineup. When a subchannel splits one call
/// sign across several guide numbers (19.1 / 19.2 / 19.3) the lowest wins — that is the main feed.
/// </summary>
public sealed class LineupIndex
{
    private readonly Dictionary<string, string> _guideByCallSign;
    private readonly HashSet<string> _guideNumbers;

    private LineupIndex(Dictionary<string, string> guideByCallSign, HashSet<string> guideNumbers)
    {
        _guideByCallSign = guideByCallSign;
        _guideNumbers = guideNumbers;
    }

    public static LineupIndex Build(IEnumerable<IptvChannel> channels)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var channel in channels)
        {
            numbers.Add(channel.GuideNumber);

            if (CallSign.Normalize(channel.CallSign) is not { } call)
                continue;

            if (!map.TryGetValue(call, out var existing) || LessThan(channel.GuideNumber, existing))
                map[call] = channel.GuideNumber;
        }

        return new LineupIndex(map, numbers);
    }

    /// <summary>The guide number for a call sign, or <c>null</c> when the lineup has no such channel.</summary>
    public string? GuideNumberFor(string? callSign) =>
        CallSign.Normalize(callSign) is { } call && _guideByCallSign.TryGetValue(call, out var guide)
            ? guide
            : null;

    /// <summary>Whether this exact guide number is tunable. Lets a caller trust a number it was handed.</summary>
    public bool HasGuideNumber(string? guideNumber) =>
        guideNumber is { Length: > 0 } && _guideNumbers.Contains(guideNumber);

    /// <summary>Numeric compare of dotted guide numbers, so "19.2" &lt; "19.10" and "2.1" &lt; "19.1".</summary>
    private static bool LessThan(string a, string b)
    {
        var pa = a.Split('.');
        var pb = b.Split('.');

        for (var i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            var na = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
            var nb = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
            if (na != nb) return na < nb;
        }

        return false;
    }
}

public static class BroadcasterClassification
{
    /// <summary>
    /// Decides whether a broadcaster is a local affiliate (a W/K call-sign token exists) and, if that
    /// call sign is in the lineup, points it at the guide number so the launcher tunes it. Never
    /// touches a broadcaster that already has a hand-set mapping.
    /// </summary>
    public static void ClassifyAgainstLineup(Broadcaster broadcaster, LineupIndex lineup)
    {
        if (broadcaster.MapsToBroadcasterId is not null || broadcaster.IptvGuideNumber is { Length: > 0 })
            return;

        var callSign = CallSign.Extract(broadcaster.CallLetters, broadcaster.Slug, broadcaster.Name);
        broadcaster.SetAffiliate(callSign is not null);

        if (callSign is not null && lineup.GuideNumberFor(callSign) is { } guide)
            broadcaster.SetMapping(null, guide);
    }
}
