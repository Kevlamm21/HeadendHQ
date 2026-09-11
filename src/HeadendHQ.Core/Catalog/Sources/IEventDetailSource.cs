namespace HeadendHQ.Core.Catalog.Sources;

public interface IEventDetailSource
{
    string SourceKey { get; }

    Task<EventDetailDescriptor?> GetDetailAsync(EventKey key, CancellationToken ct);
}
