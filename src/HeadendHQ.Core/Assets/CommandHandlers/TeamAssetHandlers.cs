using HeadendHQ.Core.Assets.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Assets.CommandHandlers;

public record GetTeamAssetsQuery : ICommand<List<TeamAsset>>;

public class GetTeamAssetsHandler(IReadModel readModel)
    : ICommandHandler<GetTeamAssetsQuery, List<TeamAsset>>
{
    public async ValueTask<List<TeamAsset>> Handle(GetTeamAssetsQuery query, CancellationToken ct)
    {
        var results = await readModel.All<TeamAsset>(ct);
        return [.. results];
    }
}

public record CreateTeamAssetCommand(
    string TeamName,
    League League,
    string? PrimaryColorHex,
    string? SecondaryColorHex) : ICommand<TeamAsset>;

public class CreateTeamAssetHandler(IWorkspace workspace)
    : ICommandHandler<CreateTeamAssetCommand, TeamAsset>
{
    public async ValueTask<TeamAsset> Handle(CreateTeamAssetCommand command, CancellationToken ct)
    {
        var existing = await workspace.LoadSingleOrDefault(
            new TeamAssetByNameLeagueSpec(command.TeamName, command.League), ct);

        if (existing is not null)
        {
            existing.Update(command.TeamName, command.League, command.PrimaryColorHex, command.SecondaryColorHex, null);
            return existing;
        }

        var asset = new TeamAsset(command.TeamName, command.League)
        {
            PrimaryColorHex = command.PrimaryColorHex,
            SecondaryColorHex = command.SecondaryColorHex
        };
        workspace.Add(asset);
        return asset;
    }
}

public record UpdateTeamAssetCommand(
    int Id,
    string? TeamName,
    League? League,
    string? PrimaryColorHex,
    string? SecondaryColorHex) : ICommand<TeamAsset>;

public class UpdateTeamAssetHandler(IWorkspace workspace)
    : ICommandHandler<UpdateTeamAssetCommand, TeamAsset>
{
    public async ValueTask<TeamAsset> Handle(UpdateTeamAssetCommand command, CancellationToken ct)
    {
        var asset = await workspace.LoadById<TeamAsset, int>(command.Id, ct);
        asset.Update(command.TeamName, command.League, command.PrimaryColorHex, command.SecondaryColorHex, null);
        return asset;
    }
}

public record UploadTeamLogoCommand(string TeamName, League League, byte[] LogoData) : ICommand<TeamAsset>;

public class UploadTeamLogoHandler(IWorkspace workspace, IImageNormalizer normalizer)
    : ICommandHandler<UploadTeamLogoCommand, TeamAsset>
{
    public async ValueTask<TeamAsset> Handle(UploadTeamLogoCommand command, CancellationToken ct)
    {
        var asset = await workspace.LoadSingleOrDefault(new TeamAssetByNameLeagueSpec(command.TeamName, command.League), ct)
            ?? throw new NotFoundException<TeamAsset>($"{command.TeamName} ({command.League})");
        asset.LogoData = await normalizer.NormalizeTeamLogoAsync(command.LogoData, ct);
        return asset;
    }
}

public record DeleteTeamAssetCommand(int Id) : ICommand<Unit>;

public class DeleteTeamAssetHandler(IWorkspace workspace)
    : ICommandHandler<DeleteTeamAssetCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteTeamAssetCommand command, CancellationToken ct)
    {
        var asset = await workspace.LoadById<TeamAsset, int>(command.Id, ct);
        workspace.Remove(asset);
        return Unit.Value;
    }
}
