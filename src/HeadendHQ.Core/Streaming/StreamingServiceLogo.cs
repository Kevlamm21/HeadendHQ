namespace HeadendHQ.Core.Streaming;

public class StreamingServiceLogo
{
    private StreamingServiceLogo() { }

    internal StreamingServiceLogo(string variant, int imageId)
    {
        Variant = variant;
        ImageId = imageId;
    }

    public int Id { get; init; }
    public int StreamingServiceId { get; private set; }

    public string Variant { get; private set; } = string.Empty;

    public int ImageId { get; private set; }

    internal void PointAt(int imageId) => ImageId = imageId;
}
