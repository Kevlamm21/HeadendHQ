using HeadendHQ.Core.Assets.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Assets.CommandHandlers;

public record GetStreamingServiceAssetsQuery : ICommand<List<StreamingServiceAsset>>;

public class GetStreamingServiceAssetsHandler(IReadModel readModel)
    : ICommandHandler<GetStreamingServiceAssetsQuery, List<StreamingServiceAsset>>
{
    public async ValueTask<List<StreamingServiceAsset>> Handle(GetStreamingServiceAssetsQuery query, CancellationToken ct)
    {
        var results = await readModel.All<StreamingServiceAsset>(ct);
        return [.. results];
    }
}

public record UploadStreamingLogoCommand(StreamingService Service, byte[] LogoData) : ICommand<StreamingServiceAsset>;

public class UploadStreamingLogoHandler(IWorkspace workspace, IImageNormalizer normalizer)
    : ICommandHandler<UploadStreamingLogoCommand, StreamingServiceAsset>
{
    public async ValueTask<StreamingServiceAsset> Handle(UploadStreamingLogoCommand command, CancellationToken ct)
    {
        var asset = await workspace.LoadSingleOrDefault(new StreamingServiceAssetByServiceSpec(command.Service), ct)
            ?? throw new NotFoundException<StreamingServiceAsset>(command.Service.ToString());
        asset.LogoData = await normalizer.NormalizeStreamingLogoAsync(command.LogoData, ct);
        return asset;
    }
}
