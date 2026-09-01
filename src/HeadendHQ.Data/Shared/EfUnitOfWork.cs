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

        // Collected before the save but published only after the commit. Both halves matter: a
        // handler that dispatches out-of-process work (Hangfire) must not race ahead of the insert,
        // and a deleted entity leaves the change tracker the moment its delete is written — so
        // reading the tracker afterwards silently dropped every event a removal had raised.
        var events = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var pending = context.ChangeTracker.Entries<IEventSource>()
                .Select(e => e.Entity)
                .SelectMany(e => e.PublishEvents())
                .ToArray();

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return pending;
        });

        if (events.Length == 0)
            return;

        foreach (var @event in events)
            await mediator.Publish(@event, cancellationToken);

        // Handlers mutate tracked entities (e.g. Title.LiveJobId), so flush again.
        await context.SaveChangesAsync(cancellationToken);
    }
}
