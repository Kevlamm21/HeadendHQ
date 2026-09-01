using HeadendHQ.Core.Media;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog;

/// <summary>
/// A player, kept only so Jellyfin's cast list has names and faces. Athletes are never browsed in
/// the UI, so they are discovered from ordinary scrape traffic rather than seeded: whenever we see
/// one we upsert them, which is also how a trade heals itself.
/// </summary>
public class Athlete : IEntity<int>
{
    private Athlete() { }

    public Athlete(int leagueId, string displayName)
    {
        LeagueId = leagueId;
        DisplayName = displayName;
    }

    public int Id { get; init; }
    public int LeagueId { get; private set; }
    public int? TeamId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;
    public string? ShortName { get; private set; }
    public string? Position { get; private set; }
    public string? Jersey { get; private set; }
    public int? ExperienceYears { get; private set; }

    public ImageRef Headshot { get; private set; } = ImageRef.Empty();

    public List<ExternalRef> ExternalRefs { get; private set; } = [];

    public void Describe(
        string displayName, string? shortName, string? position, string? jersey,
        int? experienceYears, int? teamId)
    {
        DisplayName = displayName;
        ShortName = shortName;
        if (position is not null) Position = position;
        if (jersey is not null) Jersey = jersey;
        if (experienceYears is not null) ExperienceYears = experienceYears;

        // A player seen with a new team has been traded; the latest sighting wins.
        if (teamId is not null) TeamId = teamId;
    }

    public void PointHeadshotAt(string sourceKey, string sourceUrl) =>
        Headshot.PointAt(sourceKey, sourceUrl);

    public void TrackSource(string sourceKey, string externalId) =>
        ExternalRefs.Track(sourceKey, externalId);
}
