using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface IImageCreationService
{
    Task CreatePosterAsync(Title title, CancellationToken ct = default);

    /// <summary>Landscape render Jellyfin picks up as the thumbnail.</summary>
    Task CreateThumbAsync(Title title, CancellationToken ct = default);

    /// <summary>Landscape render Jellyfin picks up as the backdrop.</summary>
    Task CreateBackdropAsync(Title title, CancellationToken ct = default);

    Task CreateClearLogoAsync(Title title, CancellationToken ct = default);
}
