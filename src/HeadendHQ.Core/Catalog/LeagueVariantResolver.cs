namespace HeadendHQ.Core.Catalog;

/// <summary>
/// One rule for recognising a competition inside a league. Ordered by <see cref="Priority"/>, so a
/// championship game matches the more specific rule before the generic one.
/// </summary>
public record LeagueVariantRule(string Variant, string? NotePhrase, int? SeasonType, int Priority = 0);

/// <summary>
/// Works out which edition of a league an event belongs to, so artwork can use the right mark.
/// <para>
/// This has to be phrase matching rather than a clean field, because ESPN does not model
/// in-season tournaments structurally: an NBA Cup quarter-final still reports
/// <c>season.slug = "regular-season"</c> and <c>season.type = 2</c>, and the only thing
/// distinguishing it is the competition note reading "NBA Cup - Quarterfinals". The rules are data
/// so a new tournament needs a row, not a release.
/// </para>
/// </summary>
public static class LeagueVariantResolver
{
    private const int Preseason = 1;
    private const int Postseason = 3;

    private static readonly LeagueVariantRule[] DefaultRules =
    [
        // Most specific first: the Cup final is still a "cup" note, so it has to win outright.
        new(LogoVariants.Finals, "cup championship", null, Priority: 30),
        new(LogoVariants.Cup, "cup", null, Priority: 20),
        new(LogoVariants.Finals, "finals", Postseason, Priority: 15),
        new(LogoVariants.Playoffs, null, Postseason, Priority: 10),
        new(LogoVariants.Preseason, null, Preseason, Priority: 5),
    ];

    /// <summary>
    /// Picks a variant from the event's note and season type. Falls back to
    /// <see cref="LogoVariants.Default"/>, which every league is guaranteed to have.
    /// </summary>
    public static string Resolve(string? note, int? seasonType, IEnumerable<LeagueVariantRule>? rules = null)
    {
        var normalized = note?.ToLowerInvariant();

        return (rules ?? DefaultRules)
            .Where(rule => Matches(rule, normalized, seasonType))
            .OrderByDescending(rule => rule.Priority)
            .Select(rule => rule.Variant)
            .FirstOrDefault() ?? LogoVariants.Default;
    }

    private static bool Matches(LeagueVariantRule rule, string? note, int? seasonType)
    {
        if (rule.NotePhrase is { } phrase)
        {
            if (note is null || !note.Contains(phrase, StringComparison.Ordinal))
                return false;
        }

        // A rule may additionally require a season type; one with neither condition never matches.
        if (rule.SeasonType is { } required && seasonType != required)
            return false;

        return rule.NotePhrase is not null || rule.SeasonType is not null;
    }
}
