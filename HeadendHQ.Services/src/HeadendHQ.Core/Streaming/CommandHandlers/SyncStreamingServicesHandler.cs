using HeadendHQ.Core.Iptv;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Streaming.CommandHandlers;

public record SyncStreamingServicesCommand : ICommand<int>;

public class SyncStreamingServicesHandler(IEnumerable<IAdbExtractor> extractors, IWorkspace workspace)
    : ICommandHandler<SyncStreamingServicesCommand, int>
{
    public async ValueTask<int> Handle(SyncStreamingServicesCommand command, CancellationToken ct)
    {
        var existing = (await workspace.LoadAll<StreamingService>(ct))
            .ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var extractor in extractors)
            Upsert(StreamingService.ForDeepLink(extractor.ProviderKey, extractor.Name));

        foreach (var channel in await workspace.LoadAll<IptvChannel>(ct))
            Upsert(StreamingService.ForIptv(
                channel.GuideNumber,
                $"{channel.GuideNumber} {channel.DisplayName ?? channel.CallSign}".TrimEnd()));

        return created;

        void Upsert(StreamingService candidate)
        {
            if (existing.TryGetValue(candidate.Key, out var service))
            {
                service.Describe(candidate.Name);
                return;
            }

            workspace.Add(candidate);
            existing[candidate.Key] = candidate;
            created++;
        }
    }
}
