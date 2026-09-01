namespace HeadendHQ.Core.Catalog;

/// <summary>
/// Which edition of a league's identity an image belongs to. Deliberately strings, not an enum:
/// leagues invent competitions (the NBA Cup arrived in 2023) and a new one must not need a release.
/// </summary>
public static class LogoVariants
{
    public const string Default = "Default";
    public const string Preseason = "Preseason";
    public const string Cup = "Cup";
    public const string Playoffs = "Playoffs";
    public const string Finals = "Finals";
}

/// <summary>
/// ESPN's <c>rel</c> tokens, normalized to a single sorted key. An open set — ESPN ships sixteen
/// variants per team today and adds more, so these are the ones we name, not the ones we accept.
/// </summary>
public static class LogoRels
{
    public const string Default = "default";
    public const string Dark = "dark";
    public const string Scoreboard = "scoreboard";
    public const string OnPrimaryColor = "primary_logo_on_primary_color";
    public const string OnSecondaryColor = "primary_logo_on_secondary_color";
    public const string OnWhiteColor = "primary_logo_on_white_color";
    public const string OnBlackColor = "primary_logo_on_black_color";
    public const string White = "primary_logo_white";
    public const string Black = "primary_logo_black";

    /// <summary>
    /// Collapses a <c>rel</c> array to a stable key. ESPN always includes "full" alongside the
    /// meaningful token, so it is dropped; what is left is sorted so ["full","scoreboard","dark"]
    /// and ["dark","scoreboard"] agree.
    /// </summary>
    public static string Normalize(IEnumerable<string> rel)
    {
        var tokens = rel
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToLowerInvariant())
            .Where(r => r != "full")
            .Distinct()
            .Order()
            .ToArray();

        return tokens.Length == 0 ? Default : string.Join("|", tokens);
    }
}
