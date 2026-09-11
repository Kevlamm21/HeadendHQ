namespace HeadendHQ.Core.Catalog.Leagues;

public record LeagueVariantRule(string Variant, string? NotePhrase, int? SeasonType, int Priority = 0);

public static class LeagueVariantResolver
{
    private const int Preseason = 1;
    private const int Postseason = 3;

    private static readonly LeagueVariantRule[] DefaultRules =
    [
        new(LogoVariants.Finals, "cup championship", null, Priority: 30),
        new(LogoVariants.Cup, "cup", null, Priority: 20),
        new(LogoVariants.Finals, "finals", Postseason, Priority: 15),
        new(LogoVariants.Playoffs, null, Postseason, Priority: 10),
        new(LogoVariants.Preseason, null, Preseason, Priority: 5),
    ];

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

        if (rule.SeasonType is { } required && seasonType != required)
            return false;

        return rule.NotePhrase is not null || rule.SeasonType is not null;
    }
}
