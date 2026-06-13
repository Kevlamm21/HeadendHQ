using HeadendHQ.Core.Titles;

namespace HeadendHQ.VodLauncher;

public class VodLauncherSettings
{
    public int Id { get; private set; }
    public Dictionary<TitleType, string> LibraryPaths { get; private set; } = new();

    public void Configure(Dictionary<TitleType, string> libraryPaths)
    {
        LibraryPaths = libraryPaths;
    }
}
