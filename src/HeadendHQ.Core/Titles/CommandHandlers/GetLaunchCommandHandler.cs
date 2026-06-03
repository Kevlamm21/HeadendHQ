using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record GetLaunchCommand(string Name) : ICommand<string>;

public class GetLaunchCommandHandler(IReadModel readModel)
    : ICommandHandler<GetLaunchCommand, string>
{
    public async ValueTask<string> Handle(GetLaunchCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Name must be provided.", nameof(command.Name));

        var results = await readModel.Search(new TitleByNameWithVideoSpec(command.Name), ct);
        var title = results.FirstOrDefault();

        if (title is null)
            throw new InvalidOperationException($"No title found for name '{command.Name}'.");

        if (title.AdbCommand is null)
            throw new InvalidOperationException($"No ADB command has been mapped for '{command.Name}' yet.");

        return title.AdbCommand;
    }
}
