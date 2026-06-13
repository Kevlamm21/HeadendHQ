namespace HeadendHQ.HdHomerun;

public class HdHomerunSettings
{
    public int Id { get; private set; }
    public string DeviceUrl { get; private set; } = string.Empty;

    public void Configure(string deviceUrl)
    {
        DeviceUrl = deviceUrl;
    }
}
