using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record RefreshLeagueLogosCommand(int LeagueId, bool RefreshExisting = false) : ICommand<League>;

public class RefreshLeagueLogosHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<RefreshLeagueLogosHandler> logger)
    : ICommandHandler<RefreshLeagueLogosCommand, League>
{
    public async ValueTask<League> Handle(RefreshLeagueLogosCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        if (league.HasFetchedLogos && !command.RefreshExisting)
            return league;

        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);

        LeagueDescriptor? descriptor;
        try
        {
            descriptor = await source.GetLeagueAsync(sport.Slug, league.Slug, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read league artwork for {League}.", league.Slug);
            return league;
        }

        var download = await CatalogLogoDownloader.DownloadAsync(
            mediator, LogoPolicy.League, descriptor?.Logos, ImagePurpose.LeagueLogo,
            command.RefreshExisting, ct);

        if (download.Stored.Count == 0)
        {
            logger.LogInformation("Source has no usable artwork for league {League}.", league.Slug);
            return league;
        }

        var dropped = league.StoreFetchedLogos(download);

        await unitOfWork.SaveChanges(ct);
        await mediator.Send(new DeleteOrphanedImagesCommand(dropped), ct);

        return league;
    }
}
