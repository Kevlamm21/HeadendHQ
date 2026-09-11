using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data.Shared;

public class EfUnitOfWork<TContext>(TContext context, IMediator mediator) : IUnitOfWork
    where TContext : DbContext
{
    public async Task SaveChanges(CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        var events = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var sources = context.ChangeTracker.Entries<IEventSource>()
                .Select(e => e.Entity)
                .ToArray();

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return sources.SelectMany(e => e.PublishEvents()).ToArray();
        });

        if (events.Length == 0)
            return;

        foreach (var @event in events)
            await mediator.Publish(@event, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
