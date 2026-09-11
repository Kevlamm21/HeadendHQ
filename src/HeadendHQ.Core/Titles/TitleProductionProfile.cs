namespace HeadendHQ.Core.Titles;

public sealed record TitleProductionProfile(bool ComposesArtwork, bool WritesNfo, bool GoesLive)
{
    public static TitleProductionProfile For(TitleType type) => type switch
    {
        TitleType.SportingEvent => new(ComposesArtwork: true, WritesNfo: true, GoesLive: true),
        TitleType.VideoGame => new(ComposesArtwork: false, WritesNfo: true, GoesLive: false),
        _ => new(ComposesArtwork: false, WritesNfo: false, GoesLive: false),
    };

    public static readonly TitleType[] ComposedArtworkTypes =
        [.. Enum.GetValues<TitleType>().Where(type => For(type).ComposesArtwork)];
}
