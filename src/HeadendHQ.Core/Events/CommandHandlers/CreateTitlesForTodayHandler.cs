using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record CreateTitlesForTodayCommand(int LeadDays = 0) : ICommand<int>;

/// <summary>
/// The daily sweep: finds every event that is due and still has no title, and produces one.
/// <para>
/// Titles are made on the day rather than for the whole scrape window, so a week of VOD folders and
/// dummy videos does not accumulate ahead of time. The mapping itself lives in
/// <see cref="ProduceTitleForEventHandler"/>; this only decides what is due.
/// </para>
/// <para>
/// A safety net as much as a driver — an event whose detail lands during the day produces its own
/// title straight away, so this mostly catches anything that was still in flight last night.
/// </para>
/// </summary>
public class CreateTitlesForTodayHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<CreateTitlesForTodayHandler> logger)
    : ICommandHandler<CreateTitlesForTodayCommand, int>
{
    public async ValueTask<int> Handle(CreateTitlesForTodayCommand command, CancellationToken ct)
    {
        // Windowed in local time: "today's games" means the user's day, not UTC's.
        var localStart = DateTime.Now.Date;
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZoneInfo.Local);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1 + command.LeadDays), TimeZoneInfo.Local);

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
                // One unproducible game must not stop tonight's other titles.
                logger.LogError(ex, "Failed to create a title for event {Id} ({Away} at {Home}).",
                    sportingEvent.Id, sportingEvent.AwayTeamName, sportingEvent.HomeTeamName);
            }
        }

        await unitOfWork.SaveChanges(ct);
        logger.LogInformation("Created {Count} title(s) for today.", created);
        return created;
    }
}
