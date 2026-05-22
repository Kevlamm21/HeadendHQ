using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record GetLaunchCommand(string Title) : ICommand<string>;

public class GetLaunchCommandHandler(ISportingEventRepository repository)
    : ICommandHandler<GetLaunchCommand, string>
{
    public async ValueTask<string> Handle(GetLaunchCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new ArgumentException("Title must be provided.", nameof(command.Title));

        var evt = await repository.GetByTitleAsync(command.Title, ct);

        if (evt is null)
            throw new InvalidOperationException($"No sporting event with a dummy video found for title '{command.Title}'.");

        if (evt.AdbCommand is null)
            throw new InvalidOperationException($"No ADB command has been mapped for '{command.Title}' yet.");

        return evt.AdbCommand;
    }
}
