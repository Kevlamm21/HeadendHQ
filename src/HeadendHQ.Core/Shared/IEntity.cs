using Mediator;
namespace HeadendHQ.Core.Shared;

public interface IEntity<TId>
{
	TId Id { get; }
}

public abstract class Entity<TId> : IEntity<TId>, IEventSource
{
	public abstract TId Id { get; init; }

	private readonly List<IEvent> _events = [];

	protected void RecordEvent(IEvent @event)
	{
		_events.Add(@event);
	}
	public IEvent[] PublishEvents()
	{
		var events = _events.ToArray();
		_events.Clear();
		return events;
	}
}
public interface IEvent : INotification;

public interface IEventSource
{
	IEvent[] PublishEvents();
}
