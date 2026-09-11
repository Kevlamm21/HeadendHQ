using HeadendHQ.Core.Catalog.Sources;

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

public record EventDetail(EventDetailDescriptor? Source, IReadOnlyList<BilledAthlete> Cast);
