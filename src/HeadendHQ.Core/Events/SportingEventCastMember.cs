namespace HeadendHQ.Core.Events;

/// <summary>
/// An athlete billed on this event, in the order they should appear.
/// <para>
/// Denormalized to exactly what a title's cast needs. The players themselves are not an entity we
/// keep: they are read out of the source's response, ranked, and the handful that get billed are
/// written down here. There is nothing to join back to.
/// </para>
/// </summary>
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

/// <summary>One player the ranker chose to bill, ready to be written down.</summary>
public record BilledAthlete(string Name, string? Role, int? HeadshotImageId);
