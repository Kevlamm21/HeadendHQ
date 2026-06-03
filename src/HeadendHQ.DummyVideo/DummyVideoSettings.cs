using HeadendHQ.Core.Titles;

namespace HeadendHQ.DummyVideo;

public class DummyVideoSettings
{
    public int Id { get; private set; }
    public Dictionary<TitleType, string> LibraryPaths { get; private set; } = new();
    public string CronSchedule { get; private set; } = "30 5 * * *";
    public bool RunOnStartup { get; private set; } = false;

    public void Configure(
        Dictionary<TitleType, string> libraryPaths,
        string cronSchedule,
        bool runOnStartup)
    {
        LibraryPaths = libraryPaths;
        CronSchedule = cronSchedule;
        RunOnStartup = runOnStartup;
    }
}
