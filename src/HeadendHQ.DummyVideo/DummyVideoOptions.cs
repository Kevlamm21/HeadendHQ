namespace HeadendHQ.Core.SportingEvents;

public class DummyVideoOptions
{
    public const string SectionName = "DummyVideo";

    public string LibraryPath { get; set; } = string.Empty;
    public string FfmpegPath { get; set; } = "ffmpeg";
    public TimeOnly RunTime { get; set; } = new TimeOnly(5, 30);
}
