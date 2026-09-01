namespace HeadendHQ.Core.Events;

/// <summary>
/// Hands an event off for its detail lookup to happen outside the scrape.
/// <para>
/// A port rather than a direct Hangfire call so the domain stays free of the job runner, and so the
/// scrape can be tested without one.
/// </para>
/// </summary>
public interface IEventDetailQueue
{
    void Enqueue(Guid sportingEventId);
}
