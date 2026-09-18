using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Iptv.CommandHandlers;

public record ParseIptvGuideCommand : ICommand<int>;

public class ParseIptvGuideHandler(
    IIptvService service,
    IWorkspace workspace,
    ILogger<ParseIptvGuideHandler> logger) : ICommandHandler<ParseIptvGuideCommand, int>
{
    public async ValueTask<int> Handle(ParseIptvGuideCommand command, CancellationToken ct)
    {
        var content = await service.GetGuideContentAsync(ct);

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("No programme guide has been cached yet; nothing to parse.");
            return 0;
        }

        var guide = XmltvParser.Parse(content);
        var channels = await workspace.LoadAll<IptvChannel>(ct);

        if (channels.Count == 0)
        {
            logger.LogWarning("The channel lineup is empty, so no guide entry can be tuned. Refresh the lineup first.");
            return 0;
        }

        var lineup = LineupIndex.Build(channels);

        var guideNumbersByChannelId = guide.Channels
            .Select(c => (c.Id, GuideNumber: ResolveGuideNumber(c, lineup)))
            .Where(x => x.GuideNumber is not null)
            .ToDictionary(x => x.Id, x => x.GuideNumber!, StringComparer.OrdinalIgnoreCase);

        var existing = (await workspace.LoadAll<IptvProgramme>(ct))
            .GroupBy(p => (p.ChannelId, p.StartUtc))
            .ToDictionary(g => g.Key, g => g.ToList());

        var added = 0;
        var updated = 0;

        foreach (var programme in guide.Programmes)
        {
            if (!guideNumbersByChannelId.TryGetValue(programme.ChannelId, out var guideNumber))
                continue;

            var key = (programme.ChannelId, programme.StartUtc);

            if (existing.TryGetValue(key, out var matches) && matches.Count > 0)
            {
                var row = matches[^1];
                matches.RemoveAt(matches.Count - 1);

                row.Update(guideNumber, programme.Title, programme.SubTitle, programme.Description, programme.StopUtc);
                updated++;
                continue;
            }

            workspace.Add(new IptvProgramme(
                guideNumber,
                programme.ChannelId,
                programme.Title,
                programme.SubTitle,
                programme.Description,
                programme.StartUtc,
                programme.StopUtc));

            added++;
        }

        var removed = 0;

        foreach (var stale in existing.Values.SelectMany(rows => rows))
        {
            workspace.Remove(stale);
            removed++;
        }

        logger.LogInformation(
            "Parsed programme guide: {Added} added, {Updated} updated, {Removed} removed across " +
            "{Channels} tunable channel(s), from {Total} entry/entries on {DocumentChannels} channel(s) " +
            "in the document.",
            added, updated, removed, guideNumbersByChannelId.Count, guide.Programmes.Count, guide.Channels.Count);

        return added + updated;
    }

    private static string? ResolveGuideNumber(XmltvChannel channel, LineupIndex lineup)
    {
        if (lineup.HasGuideNumber(channel.Id))
            return channel.Id;

        foreach (var displayName in channel.DisplayNames)
            if (lineup.HasGuideNumber(displayName))
                return displayName;

        var candidates = new[] { channel.Id }.Concat(channel.DisplayNames).ToArray();
        return lineup.GuideNumberFor(CallSign.Extract(candidates));
    }
}
