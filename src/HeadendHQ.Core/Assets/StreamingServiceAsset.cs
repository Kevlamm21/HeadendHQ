using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Assets;

public class StreamingServiceAsset : IEntity<int>
{
    private StreamingServiceAsset() { }

    public StreamingServiceAsset(StreamingService service)
    {
        Service = service;
    }

    public int Id { get; init; }
    public StreamingService Service { get; set; }
    public byte[]? LogoData { get; set; }

    public void Update(byte[]? logoData)
    {
        if (logoData is not null)
            LogoData = logoData;
    }
}
