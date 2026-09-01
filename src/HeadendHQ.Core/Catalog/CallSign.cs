using System.Text.RegularExpressions;

namespace HeadendHQ.Core.Catalog;

/// <summary>
/// Pulls a broadcast call sign — the W/K + two-or-three-letter token — out of the many shapes ESPN
/// and an HDHomeRun lineup spell it in: <c>wxix-fox19</c>, <c>WXIX FOX19</c>, <c>WXIX-TV</c>,
/// <c>WXIX-HD</c>, <c>KTRK (ABC)</c>. Returns <c>null</c> when there is no such token
/// (<c>fox-sports-1</c>, <c>ION</c>, <c>weather</c>) — which is the signal that a broadcaster is a
/// national network rather than a local affiliate.
/// </summary>
public static partial class CallSign
{
    // W or K, then two or three letters, not immediately followed by another letter (so a longer
    // word is not clipped). Trailing digits are fine: "WUSA9" -> "WUSA".
    [GeneratedRegex(@"\b([WK][A-Z]{2,3})(?![A-Z])")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"[-\s](?:TV|DT|HD|SD|LD|CD|CA|DT2)\b")]
    private static partial Regex SubchannelSuffix();

    /// <summary>The first candidate that contains a call-sign token, normalised; <c>null</c> if none do.</summary>
    public static string? Extract(params string?[] candidates)
    {
        foreach (var candidate in candidates)
            if (Normalize(candidate) is { } callSign)
                return callSign;

        return null;
    }

    /// <summary>The call-sign token in <paramref name="raw"/>, upper-cased; <c>null</c> if there is none.</summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var cleaned = SubchannelSuffix().Replace(raw.ToUpperInvariant(), " ");
        var match = TokenPattern().Match(cleaned);
        return match.Success ? match.Groups[1].Value : null;
    }
}
