using HeadendHQ.Core.Catalog.Broadcasters.Specifications;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Iptv;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

/// <summary>
/// Records a broadcaster the schedule mentioned, resolving its full record and logos the first time.
/// Unknown broadcasters are created unsubscribed, so the settings list grows to what ESPN actually
/// airs rather than what we guessed in advance.
/// </summary>
public record ResolveBroadcasterCommand(
    string ExternalId, string Slug, string Name, BroadcasterKind Kind) : ICommand<Broadcaster>;

public class ResolveBroadcasterHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IBroadcasterCatalogSource source)
    : ICommandHandler<ResolveBroadcasterCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(ResolveBroadcasterCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadSingleOrDefault(new BroadcasterBySlugSpec(command.Slug), ct);

        LineupIndex? lineup = null;

        if (broadcaster is null)
        {
            broadcaster = new Broadcaster(command.Slug, command.Name);
            broadcaster.Describe(command.Name, null, null, command.Kind);
            workspace.Add(broadcaster);

            // Classify off the slug/name now; the call letters from the detail lookup below are a
            // stronger signal, so this runs again once they are known.
            lineup = LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));
            BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
        }
        else
        {
            broadcaster.Describe(broadcaster.Name, broadcaster.ShortName, broadcaster.CallLetters, command.Kind);
        }

        // The external id only becomes known when a schedule first names the broadcaster, which is
        // why the seeded rows — espn, prime-video, peacock — sit there with no logo however long
        // they have existed: nothing had ever told them which upstream record was theirs.
        //
        // A sighting can arrive under the broadcaster's own slug or under one of its aliases, since
        // a brand's products share a row (espnplus under espn). An alias is enough to seed the
        // record — some mark beats none — but the broadcaster's own slug outranks it and replaces
        // what the alias supplied. Either way it settles after one lookup and stops churning.
        var isCanonical = broadcaster.Slug.Equals(command.Slug, StringComparison.OrdinalIgnoreCase);
        var knownExternalId = broadcaster.ExternalIdFor(source.SourceKey);

        if (!isCanonical && knownExternalId is not null)
            return broadcaster;

        if (isCanonical && knownExternalId == command.ExternalId && !broadcaster.NeedsDetail)
            return broadcaster;

        broadcaster.TrackSource(source.SourceKey, command.ExternalId);
        await unitOfWork.SaveChanges(ct);

        var detail = await source.GetBroadcasterAsync(command.ExternalId, ct);
        if (detail is not null)
        {
            // A row stands for the brand, not whichever of its products happened to air first, so an
            // alias sighting contributes artwork but must not rename ESPN to "ESPN Unlimited".
            if (isCanonical)
                broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, command.Kind);

            // Only for networks the user actually subscribes to. The schedule names a local
            // affiliate for every regional game, and downloading a mark for each would fill the
            // store with artwork that can never reach a poster. An unsubscribed row picks its logo
            // up the moment it is subscribed.
            if (broadcaster.IsSubscribed)
                await RefreshBroadcasterLogosHandler.StoreLogoAsync(
                    broadcaster, detail.Logos, mediator, refreshExisting: false, ct);

            if (isCanonical)
            {
                lineup ??= LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));
                BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
            }
        }

        // Recorded even when the source had nothing, so a local affiliate with no artwork on file —
        // most of them — is not looked up again on every scrape.
        broadcaster.MarkDetailFetched();

        return broadcaster;
    }
}
