using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Assets;

public class TeamAsset : IEntity<int>
{
    private TeamAsset() { }

    public TeamAsset(string teamName, League league)
    {
        TeamName = teamName;
        League = league;
    }

    public int Id { get; init; }
    public string TeamName { get; set; } = string.Empty;
    public League League { get; set; }
    public byte[]? LogoData { get; set; }
    public string? PrimaryColorHex { get; set; }
    public string? SecondaryColorHex { get; set; }

    public void Update(string? teamName, League? league, string? primaryColor, string? secondaryColor, byte[]? logoData)
    {
        if (teamName is not null) TeamName = teamName;
        if (league is not null) League = league.Value;
        if (primaryColor is not null) PrimaryColorHex = primaryColor == "" ? null : primaryColor;
        if (secondaryColor is not null) SecondaryColorHex = secondaryColor == "" ? null : secondaryColor;
        if (logoData is not null) LogoData = logoData;
    }
}
