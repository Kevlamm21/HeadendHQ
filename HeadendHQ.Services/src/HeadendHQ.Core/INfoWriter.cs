using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface INfoWriter
{
    Task WriteAsync(Title title, CancellationToken ct = default);
}
