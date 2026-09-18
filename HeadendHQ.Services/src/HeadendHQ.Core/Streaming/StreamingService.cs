using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Streaming;

public enum StreamingServiceKind
{
    DeepLink,
    Iptv
}

public class StreamingService : IEntity<int>
{
    private StreamingService() { }

    private StreamingService(string key, string name, StreamingServiceKind kind)
    {
        Key = key;
        Name = name;
        Kind = kind;
    }

    public static StreamingService ForDeepLink(string providerKey, string name) =>
        new(providerKey, name, StreamingServiceKind.DeepLink) { ProviderKey = providerKey };

    public static StreamingService ForIptv(string guideNumber, string name) =>
        new($"iptv:{guideNumber}", name, StreamingServiceKind.Iptv) { GuideNumber = guideNumber };

    public int Id { get; init; }

    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public StreamingServiceKind Kind { get; private set; }

    public string? ProviderKey { get; private set; }
    public string? GuideNumber { get; private set; }

    public bool IsEnabled { get; private set; }

    public List<StreamingServiceLogo> Logos { get; private set; } = [];
    public int? LogoBroadcasterId { get; private set; }

    public string? LaunchSlug => Kind is StreamingServiceKind.Iptv ? GuideNumber : ProviderKey;

    public void Describe(string name) => Name = name;

    public void Enable(bool enabled) => IsEnabled = enabled;

    public int? LogoFor(string? variant) =>
        (FindLogo(variant) ?? FindLogo(LogoVariants.Default))?.ImageId;

    public int? SetLogo(string variant, int imageId)
    {
        variant = variant.Trim();

        if (FindLogo(variant) is not { } existing)
        {
            Logos.Add(new StreamingServiceLogo(variant, imageId));
            return null;
        }

        var replaced = existing.ImageId;
        existing.PointAt(imageId);
        return replaced == imageId ? null : replaced;
    }

    public int? RemoveLogo(string variant)
    {
        if (FindLogo(variant) is not { } logo)
            return null;

        Logos.Remove(logo);
        return logo.ImageId;
    }

    public void BorrowLogoFrom(int? broadcasterId) => LogoBroadcasterId = broadcasterId;

    private StreamingServiceLogo? FindLogo(string? variant) =>
        string.IsNullOrWhiteSpace(variant)
            ? null
            : Logos.FirstOrDefault(l => l.Variant.Equals(variant.Trim(), StringComparison.OrdinalIgnoreCase));
}
