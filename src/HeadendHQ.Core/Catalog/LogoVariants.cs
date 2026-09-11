namespace HeadendHQ.Core.Catalog;

public static class LogoVariants
{
    public const string Default = "Default";
    public const string Preseason = "Preseason";
    public const string Cup = "Cup";
    public const string Playoffs = "Playoffs";
    public const string Finals = "Finals";
}

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
