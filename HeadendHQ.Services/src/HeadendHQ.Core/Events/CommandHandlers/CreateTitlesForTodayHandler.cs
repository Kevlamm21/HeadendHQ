using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record CreateTitlesForTodayCommand(int LeadDays = 0) : ICommand<int>;

public class CreateTitlesForTodayHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<CreateTitlesForTodayHandler> logger)
    : ICommandHandler<CreateTitlesForTodayCommand, int>
{
    public async ValueTask<int> Handle(CreateTitlesForTodayCommand command, CancellationToken ct)
    {
        var (fromUtc, toUtc) = LocalDay.UtcWindow(command.LeadDays);

        var due = await workspace.Load(new EventsNeedingTitlesSpec(fromUtc, toUtc), ct);

        if (due.Count == 0)
        {
            logger.LogInformation("No sporting events need a title today.");
            return 0;
        }

        var created = 0;

        foreach (var sportingEvent in due)
        {
            try
            {
                if (await mediator.Send(new ProduceTitleForEventCommand(sportingEvent.Id), ct) is not null)
                    created++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to create a title for event {Id} ({Away} at {Home}).",
                    sportingEvent.Id, sportingEvent.AwayTeamName, sportingEvent.HomeTeamName);
            }
        }

        await unitOfWork.SaveChanges(ct);
        logger.LogInformation("Created {Count} title(s) for today.", created);
        return created;
    }
}
