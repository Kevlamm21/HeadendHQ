using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Streaming;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record StreamingAssignmentRequest(
    int? StreamingServiceId, int Priority, bool UseBroadcasterLogo = false, string? LogoVariant = null);

public record SetStreamingAssignmentsCommand(int BroadcasterId, IReadOnlyList<StreamingAssignmentRequest> Assignments)
    : ICommand<Broadcaster>;

public class SetStreamingAssignmentsHandler(IWorkspace workspace)
    : ICommandHandler<SetStreamingAssignmentsCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(SetStreamingAssignmentsCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        foreach (var serviceId in command.Assignments.Select(a => a.StreamingServiceId).OfType<int>().Distinct())
            _ = await workspace.LoadById<StreamingService, int>(serviceId, ct);

        broadcaster.SetStreamingAssignments(command.Assignments.Select(a =>
            new StreamingAssignment(a.StreamingServiceId, a.Priority, a.UseBroadcasterLogo, a.LogoVariant)));

        return broadcaster;
    }
}
