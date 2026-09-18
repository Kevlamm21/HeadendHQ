namespace HeadendHQ.Core.Settings;

public class GlobalSettings
{
    public int Id { get; private set; }
    public int TitleRetentionDays { get; private set; } = 30;

    public string? PublicBaseUrl { get; private set; }

    public void Configure(int? titleRetentionDays, string? publicBaseUrl)
    {
        if (titleRetentionDays is not null) TitleRetentionDays = titleRetentionDays.Value;
        if (publicBaseUrl is not null) PublicBaseUrl = publicBaseUrl == "" ? null : publicBaseUrl.TrimEnd('/');
    }
}
