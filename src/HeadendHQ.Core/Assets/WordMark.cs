using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Assets;

public class WordMark : IEntity<int>
{
    private WordMark() { }

    public WordMark(League league, string variant)
    {
        League = league;
        Variant = variant;
    }

    public int Id { get; init; }
    public League League { get; set; }
    public string Variant { get; set; } = "Default";
    public byte[]? LogoData { get; set; }

    public void Update(League league, string variant, byte[]? logoData)
    {
        League = league;
        Variant = variant;
        if (logoData is not null)
            LogoData = logoData;
    }
}
