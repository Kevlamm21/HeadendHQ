using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Iptv;

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

    public string GuideNumber { get; private set; } = string.Empty;

    public string ChannelId { get; private set; } = string.Empty;

    public string? Title { get; private set; }

    public string? SubTitle { get; private set; }
    public string? Description { get; private set; }

    public DateTime StartUtc { get; private set; }
    public DateTime StopUtc { get; private set; }

    public void Update(string guideNumber, string? title, string? subTitle, string? description, DateTime stopUtc)
    {
        GuideNumber = guideNumber;
        Title = title;
        SubTitle = subTitle;
        Description = description;
        StopUtc = stopUtc;
    }
}
