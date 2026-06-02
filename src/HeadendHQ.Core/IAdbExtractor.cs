using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface IAdbExtractor
{
    StreamingService Service { get; }
    string BuildCommand(string eventUrl);
}
