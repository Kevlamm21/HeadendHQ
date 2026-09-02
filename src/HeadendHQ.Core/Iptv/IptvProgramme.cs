using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Iptv;

/// <summary>
/// One entry from the programme guide, tied to a tunable channel.
/// <para>
/// The guide is the only thing that knows which of several simultaneous regional games a local
/// affiliate is actually carrying: FOX airs Bengals–Ravens and Chargers–Bears in the same slot, and
/// only one of them reaches WXIX. Storing the guide as rows rather than as the raw XMLTV document is
/// what lets a game be checked against the channel it is supposed to be tuned on.
/// </para>
/// <para>
/// A row is identified by the channel it airs on and when it starts. The guide is refetched nightly
/// and mostly repeats itself, so entries are reconciled against that pair rather than replaced
/// wholesale — which keeps a fortnight of listings from being deleted and reinserted every night.
/// </para>
/// </summary>
public class IptvProgramme : IEntity<int>
{
    private IptvProgramme() { }

    public IptvProgramme(
        string guideNumber,
        string channelId,
        string? title,
        string? subTitle,
        string? description,
        DateTime startUtc,
        DateTime stopUtc)
    {
        GuideNumber = guideNumber;
        ChannelId = channelId;
        Title = title;
        SubTitle = subTitle;
        Description = description;
        StartUtc = startUtc;
        StopUtc = stopUtc;
    }

    public int Id { get; init; }

    /// <summary>
    /// The lineup guide number this airs on, e.g. "19.1". Resolved from the XMLTV channel id at
    /// parse time so the regional check can query by the same key the launcher tunes.
    /// </summary>
    public string GuideNumber { get; private set; } = string.Empty;

    /// <summary>The XMLTV <c>channel</c> attribute, kept for tracing a row back to the document.</summary>
    public string ChannelId { get; private set; } = string.Empty;

    /// <summary>
    /// The programme title — for sport this is usually the series ("NFL Football"), with the
    /// matchup in <see cref="SubTitle"/>. Both are searched when matching a game.
    /// </summary>
    public string? Title { get; private set; }

    public string? SubTitle { get; private set; }
    public string? Description { get; private set; }

    public DateTime StartUtc { get; private set; }
    public DateTime StopUtc { get; private set; }

    /// <summary>
    /// Refreshes everything the guide can restate for an entry already on file. The channel and
    /// start time are what identify it, so they are not among them; a lineup renumbering can still
    /// move it to a different <see cref="GuideNumber"/>.
    /// </summary>
    public void Update(string guideNumber, string? title, string? subTitle, string? description, DateTime stopUtc)
    {
        GuideNumber = guideNumber;
        Title = title;
        SubTitle = subTitle;
        Description = description;
        StopUtc = stopUtc;
    }
}
