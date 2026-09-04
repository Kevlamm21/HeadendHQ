using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.CommandHandlers;

/// <summary>
/// Hand uploads. Every one of these detaches the slot from its upstream URL, so a later refresh
/// updates validators but never replaces the user's image.
/// </summary>
public record UploadLeagueWordmarkCommand(int LeagueId, string Variant, byte[] Bytes) : ICommand<League>;

public class UploadLeagueWordmarkHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadLeagueWordmarkCommand, League>
{
    public async ValueTask<League> Handle(UploadLeagueWordmarkCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);
        var wordmark = league.UpsertWordmark(command.Variant);

        await mediator.Send(new UploadImageCommand(wordmark.Image, command.Bytes, ImagePurpose.Wordmark), ct);
        return league;
    }
}

public record UploadLeagueLogoOverrideCommand(int LeagueId, string Variant, byte[] Bytes) : ICommand<League>;

public class UploadLeagueLogoOverrideHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadLeagueLogoOverrideCommand, League>
{
    public async ValueTask<League> Handle(UploadLeagueLogoOverrideCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        var logo = league.Logos.FirstOrDefault(l => l.Variant == command.Variant && l.Rel == LogoRels.Default)
            ?? league.UpsertLogo(command.Variant, LogoRels.Default, SourceKeys.Espn, string.Empty, null);

        await mediator.Send(new UploadImageCommand(logo.Image, command.Bytes, ImagePurpose.LeagueLogo), ct);
        return league;
    }
}

public record UploadTeamLogoOverrideCommand(int TeamId, string Rel, byte[] Bytes) : ICommand<Team>;

public class UploadTeamLogoOverrideHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadTeamLogoOverrideCommand, Team>
{
    public async ValueTask<Team> Handle(UploadTeamLogoOverrideCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        var logo = team.Logos.FirstOrDefault(l => l.Rel == command.Rel)
            ?? team.UpsertLogo(command.Rel, SourceKeys.Espn, string.Empty, null);

        await mediator.Send(new UploadImageCommand(logo.Image, command.Bytes, ImagePurpose.TeamLogo), ct);
        return team;
    }
}

public record UploadBroadcasterLogoCommand(int BroadcasterId, byte[] Bytes) : ICommand<Broadcaster>;

public class UploadBroadcasterLogoHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadBroadcasterLogoCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(UploadBroadcasterLogoCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        var logo = broadcaster.LogoFor(LogoVariants.Default)
            ?? broadcaster.UpsertLogo(LogoVariants.Default, SourceKeys.Espn, string.Empty, null);

        await mediator.Send(new UploadImageCommand(logo.Image, command.Bytes, ImagePurpose.BroadcasterLogo), ct);
        return broadcaster;
    }
}

/// <summary>
/// Clears the completion marker and walks the catalog again. Existing rows are matched by slug and
/// updated in place, so a re-sync corrects drift without duplicating anything.
/// </summary>
public record ForceCatalogSyncCommand : ICommand<CatalogSyncState>;

public class ForceCatalogSyncHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IMediator mediator)
    : ICommandHandler<ForceCatalogSyncCommand, CatalogSyncState>
{
    public async ValueTask<CatalogSyncState> Handle(ForceCatalogSyncCommand command, CancellationToken ct)
    {
        var state = await workspace.LoadSingleOrDefault(new CatalogSyncStateSpec(), ct)
            ?? throw new InvalidOperationException("CatalogSyncState not found.");

        state.Reset();
        await unitOfWork.SaveChanges(ct);

        return await mediator.Send(new SyncSportsAndLeaguesCommand(), ct);
    }
}
