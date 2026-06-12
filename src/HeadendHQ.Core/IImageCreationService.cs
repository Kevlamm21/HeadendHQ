using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface IImageCreationService
{
    Task<string> CreatePosterAsync(Title title, CancellationToken ct = default);
    Task<string> CreateThumbnailAsync(Title title, CancellationToken ct = default);
    Task<string> CreateBackgroundAsync(Title title, CancellationToken ct = default);
    Task<string?> CreateClearLogoAsync(Title title, CancellationToken ct = default);
}
