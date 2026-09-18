using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace HeadendHQ.Core.Iptv;

public record XmltvChannel(string Id, IReadOnlyList<string> DisplayNames);

public record XmltvProgramme(
    string ChannelId,
    string? Title,
    string? SubTitle,
    string? Description,
    DateTime StartUtc,
    DateTime StopUtc);

public record XmltvGuide(IReadOnlyList<XmltvChannel> Channels, IReadOnlyList<XmltvProgramme> Programmes);

public static class XmltvParser
{
    public static XmltvGuide Parse(string? xml)
    {
        var channels = new List<XmltvChannel>();
        var programmes = new List<XmltvProgramme>();

        if (string.IsNullOrWhiteSpace(xml))
            return new XmltvGuide(channels, programmes);

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
        };

        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, settings);

        while (!reader.EOF)
        {
            if (reader.NodeType is not XmlNodeType.Element || reader.Name is not ("channel" or "programme"))
            {
                reader.Read();
                continue;
            }

            var name = reader.Name;
            var element = (XElement)XNode.ReadFrom(reader);

            if (name is "channel")
            {
                if (ReadChannel(element) is { } channel)
                    channels.Add(channel);
            }
            else if (ReadProgramme(element) is { } programme)
            {
                programmes.Add(programme);
            }
        }

        return new XmltvGuide(channels, programmes);
    }

    private static XmltvChannel? ReadChannel(XElement element)
    {
        var id = (string?)element.Attribute("id");
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var displayNames = element.Elements("display-name")
            .Select(e => e.Value.Trim())
            .Where(v => v.Length > 0)
            .ToList();

        return new XmltvChannel(id.Trim(), displayNames);
    }

    private static XmltvProgramme? ReadProgramme(XElement element)
    {
        var channelId = (string?)element.Attribute("channel");
        if (string.IsNullOrWhiteSpace(channelId))
            return null;

        if (ParseTimestamp((string?)element.Attribute("start")) is not { } startUtc)
            return null;

        var stopUtc = ParseTimestamp((string?)element.Attribute("stop")) ?? startUtc.AddHours(3);

        return new XmltvProgramme(
            channelId.Trim(),
            Text(element, "title"),
            Text(element, "sub-title"),
            Text(element, "desc"),
            startUtc,
            stopUtc);
    }

    private static string? Text(XElement element, string name) =>
        element.Elements(name)
            .Select(e => e.Value.Trim())
            .FirstOrDefault(v => v.Length > 0);

    public static DateTime? ParseTimestamp(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var value = raw.Trim();

        var split = value.IndexOf(' ');
        var digits = (split < 0 ? value : value[..split]).Trim();
        var zone = split < 0 ? null : value[(split + 1)..].Trim();

        var format = digits.Length switch
        {
            14 => "yyyyMMddHHmmss",
            12 => "yyyyMMddHHmm",
            8 => "yyyyMMdd",
            _ => null,
        };

        if (format is null ||
            !DateTime.TryParseExact(digits, format, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var local))
            return null;

        if (ParseOffset(zone) is not { } offset)
            return DateTime.SpecifyKind(local, DateTimeKind.Utc);

        return new DateTimeOffset(local, offset).UtcDateTime;
    }

    private static TimeSpan? ParseOffset(string? zone)
    {
        if (string.IsNullOrWhiteSpace(zone))
            return null;

        var value = zone.Replace(":", "");
        if (value.Length != 5 || (value[0] is not ('+' or '-')))
            return null;

        if (!int.TryParse(value.AsSpan(1, 2), out var hours) ||
            !int.TryParse(value.AsSpan(3, 2), out var minutes))
            return null;

        var offset = new TimeSpan(hours, minutes, 0);
        return value[0] is '-' ? -offset : offset;
    }
}
