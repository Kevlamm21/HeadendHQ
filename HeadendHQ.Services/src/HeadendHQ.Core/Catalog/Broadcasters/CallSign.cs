using System.Text.RegularExpressions;

namespace HeadendHQ.Core.Catalog.Broadcasters;

public static partial class CallSign
{
    [GeneratedRegex(@"\b([WK][A-Z]{2,3})(?![A-Z])")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"[-\s](?:TV|DT|HD|SD|LD|CD|CA|DT2)\b")]
    private static partial Regex SubchannelSuffix();

    public static string? Extract(params string?[] candidates)
    {
        foreach (var candidate in candidates)
            if (Normalize(candidate) is { } callSign)
                return callSign;

        return null;
    }

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var cleaned = SubchannelSuffix().Replace(raw.ToUpperInvariant(), " ");
        var match = TokenPattern().Match(cleaned);
        return match.Success ? match.Groups[1].Value : null;
    }
}
