using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface IImageCreationService
{
    Task CreatePosterAsync(Title title, CancellationToken ct = default);
    Task CreateThumbnailAsync(Title title, CancellationToken ct = default);
    Task CreateBackgroundAsync(Title title, CancellationToken ct = default);
    Task CreateClearLogoAsync(Title title, CancellationToken ct = default);
}
