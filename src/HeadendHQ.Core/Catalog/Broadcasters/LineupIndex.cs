using HeadendHQ.Core.Iptv;

namespace HeadendHQ.Core.Catalog.Broadcasters;

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

    public string? GuideNumberFor(string? callSign) =>
        CallSign.Normalize(callSign) is { } call && _guideByCallSign.TryGetValue(call, out var guide)
            ? guide
            : null;

    public bool HasGuideNumber(string? guideNumber) =>
        guideNumber is { Length: > 0 } && _guideNumbers.Contains(guideNumber);

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
