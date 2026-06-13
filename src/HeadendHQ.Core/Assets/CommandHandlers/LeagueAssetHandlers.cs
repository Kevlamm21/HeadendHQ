using HeadendHQ.Core.Assets.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Assets.CommandHandlers;

public record GetLeagueAssetsQuery : ICommand<List<LeagueAsset>>;

public class GetLeagueAssetsHandler(IReadModel readModel)
    : ICommandHandler<GetLeagueAssetsQuery, List<LeagueAsset>>
{
    public async ValueTask<List<LeagueAsset>> Handle(GetLeagueAssetsQuery query, CancellationToken ct)
    {
        var results = await readModel.All<LeagueAsset>(ct);
        return [.. results];
    }
}

public record CreateLeagueAssetCommand(League League, string Variant) : ICommand<LeagueAsset>;

public class CreateLeagueAssetHandler(IWorkspace workspace)
    : ICommandHandler<CreateLeagueAssetCommand, LeagueAsset>
{
    public async ValueTask<LeagueAsset> Handle(CreateLeagueAssetCommand command, CancellationToken ct)
    {
        var existing = await workspace.LoadSingleOrDefault(
            new LeagueAssetByLeagueVariantSpec(command.League, command.Variant), ct);

        if (existing is not null)
            return existing;

        var asset = new LeagueAsset(command.League, command.Variant);
        workspace.Add(asset);
        return asset;
    }
}

public record UploadLeagueLogoCommand(League League, string Variant, byte[] LogoData) : ICommand<LeagueAsset>;

public class UploadLeagueLogoHandler(IWorkspace workspace, IImageNormalizer normalizer)
    : ICommandHandler<UploadLeagueLogoCommand, LeagueAsset>
{
    public async ValueTask<LeagueAsset> Handle(UploadLeagueLogoCommand command, CancellationToken ct)
    {
        var asset = await workspace.LoadSingleOrDefault(new LeagueAssetByLeagueVariantSpec(command.League, command.Variant), ct)
            ?? throw new NotFoundException($"League asset '{command.League}' ({command.Variant}) not found.");
        asset.LogoData = await normalizer.NormalizeLeagueLogoAsync(command.LogoData, ct);
        return asset;
    }
}

public record DeleteLeagueAssetCommand(int Id) : ICommand<Unit>;

public class DeleteLeagueAssetHandler(IWorkspace workspace)
    : ICommandHandler<DeleteLeagueAssetCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteLeagueAssetCommand command, CancellationToken ct)
    {
        var asset = await workspace.LoadById<LeagueAsset, int>(command.Id, ct);
        workspace.Remove(asset);
        return Unit.Value;
    }
}
