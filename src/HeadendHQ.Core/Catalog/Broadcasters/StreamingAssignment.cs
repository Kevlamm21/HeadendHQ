namespace HeadendHQ.Core.Catalog.Broadcasters;

public class StreamingAssignment
{
    private StreamingAssignment() { }

    public StreamingAssignment(int? streamingServiceId, int priority, bool useBroadcasterLogo, string? logoVariant)
    {
        StreamingServiceId = streamingServiceId;
        Priority = priority;
        UseBroadcasterLogo = useBroadcasterLogo;
        LogoVariant = string.IsNullOrWhiteSpace(logoVariant) ? null : logoVariant.Trim();
    }

    public int Id { get; init; }
    public int BroadcasterId { get; private set; }

    public int? StreamingServiceId { get; private set; }

    public int Priority { get; private set; }

    public bool UseBroadcasterLogo { get; private set; }

    public string? LogoVariant { get; private set; }
}
