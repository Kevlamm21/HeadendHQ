using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

/// <summary>
/// Downloads a league's own marks. Split out because the league catalog records no artwork — 356
/// leagues would mean 356 downloads for the handful anyone follows — so this is what a follow, and
/// later a seed, calls to fill one league in.
/// </summary>
public record RefreshLeagueLogosCommand(int LeagueId, bool RefreshExisting = false) : ICommand<int>;

public class RefreshLeagueLogosHandler(
    IWorkspace workspace,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<RefreshLeagueLogosHandler> logger)
    : ICommandHandler<RefreshLeagueLogosCommand, int>
{
    public async ValueTask<int> Handle(RefreshLeagueLogosCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        if (league.LogoFor(LogoVariants.Default) is { } held
            && (!command.RefreshExisting || held.Origin is ImageOrigin.Manual))
            return 0;

        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);

        IReadOnlyList<LeagueDescriptor> descriptors;
        try
        {
            // One request returns every league in the sport; the source has no cheaper way to ask
            // about a single one, and this only runs when a league gains a follower.
            descriptors = await source.GetLeaguesAsync(sport.Slug, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read league artwork for {League}.", league.Slug);
            return 0;
        }

        var descriptor = descriptors.FirstOrDefault(d =>
            d.Slug.Equals(league.Slug, StringComparison.OrdinalIgnoreCase));

        if (LogoSelection.ForLeague(descriptor?.Logos) is not { } chosen)
        {
            logger.LogInformation("Source has no artwork for league {League}.", league.Slug);
            return 0;
        }

        if (await mediator.Send(
                new MaterializeImageByUrlCommand(
                    chosen.Url, ImagePurpose.LeagueLogo, league.Id, command.RefreshExisting), ct)
            is not { } imageId)
            return 0;

        league.UpsertLogo(LogoVariants.Default, LogoSelection.LabelFor(chosen), imageId);
        return 1;
    }
}
