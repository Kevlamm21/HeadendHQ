namespace HeadendHQ.Core.Settings;

/// <summary>How an NFO points at a cast member's headshot.</summary>
public enum ActorThumbMode
{
    /// <summary>A path relative to the title folder, resolved against files written beside the NFO.</summary>
    LocalFile,

    /// <summary>An absolute URL served by this application, which the media server must be able to reach.</summary>
    Url
}

public class GlobalSettings
{
    public int Id { get; private set; }
    public int TitleRetentionDays { get; private set; } = 30;

    public string? PublicBaseUrl { get; private set; }

    /// <summary>
    /// Defaults to the local file because a URL only works if the media server can reach this
    /// application, and <see cref="PublicBaseUrl"/> pointing at <c>localhost</c> means the media
    /// server's own container rather than ours. Switch to <see cref="ActorThumbMode.Url"/> if the
    /// media server turns out to insist on http for person images.
    /// </summary>
    public ActorThumbMode ActorThumbMode { get; private set; } = ActorThumbMode.LocalFile;

    public void Configure(int? titleRetentionDays, string? publicBaseUrl, ActorThumbMode? actorThumbMode = null)
    {
        if (titleRetentionDays is not null) TitleRetentionDays = titleRetentionDays.Value;
        if (publicBaseUrl is not null) PublicBaseUrl = publicBaseUrl == "" ? null : publicBaseUrl.TrimEnd('/');
        if (actorThumbMode is not null) ActorThumbMode = actorThumbMode.Value;
    }
}
