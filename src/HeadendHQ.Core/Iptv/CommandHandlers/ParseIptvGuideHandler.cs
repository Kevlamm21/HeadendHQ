using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Iptv.CommandHandlers;

public record ParseIptvGuideCommand : ICommand<int>;

/// <summary>
/// Turns the cached XMLTV document into <see cref="IptvProgramme"/> rows.
/// <para>
/// Parsing lives here rather than in the device module because the format is a standard and the
/// device is not — <see cref="IIptvService"/> fetches, Core makes sense of what came back.
/// </para>
/// <para>
/// Only channels that resolve to a lineup guide number are kept. The one question these rows answer
/// is "is this game on the channel we are about to tune", which is asked by guide number, so a
/// programme that cannot be tied to one is weight without value — and a full fortnight of guide for
/// every channel the document mentions is a great deal of weight.
/// </para>
/// </summary>
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

        // Reconciled against (channel, start) rather than replaced wholesale: the guide is refetched
        // nightly and mostly repeats itself, and the existing rows have to be read either way, so
        // updating in place spares a fortnight of listings from a delete-and-reinsert every night.
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

            // A well-formed document never lists two programmes at the same instant on one channel,
            // but a malformed one must not resurrect a row that is about to be pruned.
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

    /// <summary>
    /// An XMLTV channel id is whatever the publisher chose. HDHomeRun uses the guide number itself,
    /// so that is tried first; otherwise the call sign in the id or any display-name is matched
    /// against the lineup, which is how "WXIX-TV" or "19.1 WXIX (FOX)" still finds 19.1.
    /// </summary>
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
