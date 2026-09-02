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

        // The event sources are captured before the save — a deleted entity leaves the change
        // tracker the moment its delete is written, so reading the tracker afterwards silently
        // dropped every event a removal had raised. But the events are only *drained* (which clears
        // them from the entity) after the commit succeeds: if the save throws, the entity stays
        // tracked with its events intact, and a retry — or a later SaveChanges on the same context,
        // as the nightly job does after catching a per-item failure — still publishes them instead
        // of persisting the row silently. Publishing only after the commit also keeps a handler that
        // dispatches out-of-process work (Hangfire) from racing ahead of the insert.
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

        // Handlers mutate tracked entities (e.g. Title.LiveJobId), so flush again.
        await context.SaveChangesAsync(cancellationToken);
    }
}
