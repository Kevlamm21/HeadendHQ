namespace HeadendHQ.Core.Events;

public class SportingEventCastMember
{
    private SportingEventCastMember() { }

    internal SportingEventCastMember(string name, string? role, int? headshotImageId, int order)
    {
        Name = name;
        Role = role;
        HeadshotImageId = headshotImageId;
        Order = order;
    }

    public int Id { get; init; }
    public Guid SportingEventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Role { get; private set; }
    public int? HeadshotImageId { get; private set; }
    public int Order { get; private set; }
}

public record BilledAthlete(string Name, string? Role, int? HeadshotImageId);

public record EventDetail(EventDetailRequest? Source, IReadOnlyList<BilledAthlete> Cast);

public record AthleteRequest(
    string ExternalId,
    string DisplayName,
    string? Position = null,
    int? ExperienceYears = null,
    string? HeadshotUrl = null,
    int? DepthRank = null,
    string? InjuryStatus = null);

public record CastRequest(
    AthleteRequest Athlete,
    bool IsHome,
    bool IsListedStarter = false,
    bool IsStatLeader = false,
    bool IsProbableStarter = false);
