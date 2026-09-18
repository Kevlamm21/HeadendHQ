namespace HeadendHQ.Core;

public interface IImageCreationService
{
    Task<int> CreatePosterAsync(Guid sourceId, CancellationToken ct = default);

    Task<int> CreateThumbAsync(Guid sourceId, CancellationToken ct = default);

    Task<int> CreateBackdropAsync(Guid sourceId, CancellationToken ct = default);
}
