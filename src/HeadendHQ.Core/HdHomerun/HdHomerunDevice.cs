namespace HeadendHQ.Core.HdHomerun;

public class HdHomerunDevice
{
    public int Id { get; set; }
    public string DeviceAuth { get; set; } = string.Empty;
    public string XmltvUrl { get; set; } = string.Empty;
    public string? XmltvContent { get; set; }
    public DateTime? LastUpdated { get; set; }
}
