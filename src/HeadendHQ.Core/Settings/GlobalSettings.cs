using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Settings;

public class GlobalSettings
{
    public int Id { get; private set; }
    public List<StreamingService> EnabledStreamingServices { get; private set; } = [.. Enum.GetValues<StreamingService>()];
    public int TitleRetentionDays { get; private set; } = 30;

    public void Configure(List<StreamingService>? enabledStreamingServices, int titleRetentionDays)
    {
        EnabledStreamingServices = enabledStreamingServices ?? [];
        TitleRetentionDays = titleRetentionDays;
    }
}
