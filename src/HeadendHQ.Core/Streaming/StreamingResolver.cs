using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Iptv;
using HeadendHQ.Core.Iptv.Specifications;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Streaming;

public enum StreamingOutcome
{
    None,
    Stream,
    Blocked
}

public record StreamingChoice(
    StreamingOutcome Outcome,
    Broadcaster? Broadcaster = null,
    StreamingAssignment? Assignment = null,
    StreamingService? Service = null);

public static class StreamingResolver
{
    private static readonly TimeSpan GuideLeadIn = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan GuideLookAhead = TimeSpan.FromMinutes(30);

    public static async Task<StreamingChoice> ResolveAsync(
        IReadModel readModel,
        DateTime startUtc,
        string homeTeamName,
        string awayTeamName,
        IReadOnlyList<(Broadcaster Broadcaster, int Priority)> broadcasts,
        CancellationToken ct)
    {
        var services = (await readModel.All<StreamingService>(ct)).ToDictionary(s => s.Id);

        var candidates = broadcasts
            .SelectMany(b => b.Broadcaster.StreamingAssignments.Select(a => (b.Broadcaster, Assignment: a, b.Priority)))
            .Where(c => c.Assignment.StreamingServiceId is not { } id || services.GetValueOrDefault(id)?.IsEnabled == true)
            .OrderBy(c => c.Assignment.Priority)
            .ThenBy(c => c.Priority);

        foreach (var (broadcaster, assignment, _) in candidates)
        {
            if (assignment.StreamingServiceId is not { } serviceId)
                return new StreamingChoice(StreamingOutcome.Blocked, broadcaster, assignment);

            var service = services[serviceId];

            // TODO(deep-links): look the link up in the deep-link catalog and continue to the next assignment when
            // the provider has no stream, never walking past a Blocked entry.

            if (service.Kind is StreamingServiceKind.DeepLink
                || await GuideCarriesAsync(readModel, service.GuideNumber!, startUtc, homeTeamName, awayTeamName, ct))
                return new StreamingChoice(StreamingOutcome.Stream, broadcaster, assignment, service);
        }

        return new StreamingChoice(StreamingOutcome.None);
    }

    private static async Task<bool> GuideCarriesAsync(
        IReadModel readModel, string guideNumber, DateTime startUtc, string homeTeamName, string awayTeamName,
        CancellationToken ct)
    {
        var programmes = await readModel.Search(
            new ProgrammesOnChannelSpec(guideNumber, startUtc - GuideLeadIn, startUtc + GuideLookAhead), ct);

        // An empty window means the guide doesn't reach that far yet; it is re-checked on the day.
        if (programmes.Count == 0)
            return true;

        string[] names = [.. TeamTerms(homeTeamName), .. TeamTerms(awayTeamName)];
        return programmes.Any(p => names.Any(name => Mentions(p, name)));
    }

    private static IEnumerable<string> TeamTerms(string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            yield break;

        yield return teamName;

        var nickname = teamName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];
        if (nickname.Length > 3 && nickname != teamName)
            yield return nickname;
    }

    private static bool Mentions(IptvProgramme programme, string name) =>
        (programme.Title?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false)
        || (programme.SubTitle?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false)
        || (programme.Description?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false);
}
