namespace HeadendHQ.Core;

/// <summary>
/// Composes a sporting-event title's artwork and stores the result in the media library, returning
/// the image id. Given the source id, it resolves the league/team/broadcaster marks it needs itself.
/// </summary>
public interface IImageCreationService
{
    /// <summary>Portrait cover. Jellyfin: Primary.</summary>
    Task<int> CreatePosterAsync(Guid sourceId, CancellationToken ct = default);

    /// <summary>Landscape render Jellyfin picks up as the thumbnail.</summary>
    Task<int> CreateThumbAsync(Guid sourceId, CancellationToken ct = default);

    /// <summary>Landscape render Jellyfin picks up as the backdrop.</summary>
    Task<int> CreateBackdropAsync(Guid sourceId, CancellationToken ct = default);
}
