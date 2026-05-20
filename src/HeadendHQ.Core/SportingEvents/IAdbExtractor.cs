namespace HeadendHQ.Core.SportingEvents;

public interface IAdbExtractor
{
    StreamingService Service { get; }
    string BuildCommand(string eventUrl);
}
