namespace HeadendHQ.Core.Events;

/// <summary>An athlete billed on this event, in the order they should appear.</summary>
public class SportingEventCastMember
{
    private SportingEventCastMember() { }

    internal SportingEventCastMember(int athleteId, int order)
    {
        AthleteId = athleteId;
        Order = order;
    }

    public int Id { get; init; }
    public Guid SportingEventId { get; private set; }
    public int AthleteId { get; private set; }
    public int Order { get; private set; }
}
