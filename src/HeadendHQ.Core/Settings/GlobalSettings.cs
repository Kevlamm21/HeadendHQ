namespace HeadendHQ.Core.Settings;

public class GlobalSettings
{
    public int Id { get; private set; }
    public int TitleRetentionDays { get; private set; } = 30;

    /// <summary>
    /// Base URL this application is reachable at from the media server. Cast headshots are written
    /// into the NFO as <c>{PublicBaseUrl}/media/images/{id}</c>, so without it those thumbs are
    /// omitted.
    /// </summary>
    public string? PublicBaseUrl { get; private set; }

    public void Configure(int? titleRetentionDays, string? publicBaseUrl)
    {
        if (titleRetentionDays is not null) TitleRetentionDays = titleRetentionDays.Value;
        if (publicBaseUrl is not null) PublicBaseUrl = publicBaseUrl == "" ? null : publicBaseUrl.TrimEnd('/');
    }
}
