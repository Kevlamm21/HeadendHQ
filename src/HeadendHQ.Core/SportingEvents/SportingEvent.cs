namespace HeadendHQ.Core.SportingEvents;

public class SportingEvent
{
    private SportingEvent() { }

    public SportingEvent(SportingEventRequest request)
    {
        Title = request.Title;
        Sport = request.Sport;
        StreamingService = request.StreamingService;
        EventUrl = request.EventUrl;
        Provider = request.Provider;
        StartUtc = request.StartUtc;
        EndUtc = request.EndUtc ?? request.StartUtc.AddHours(3);
    }

    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Sport Sport { get; set; }
    public StreamingService StreamingService { get; set; }
    public string EventUrl { get; set; } = string.Empty;
    public string? AdbCommand { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public void Update(SportingEventRequest request)
    {
        Title = request.Title;
        Sport = request.Sport;
        StreamingService = request.StreamingService;
        StartUtc = request.StartUtc;
        EndUtc = request.EndUtc ?? request.StartUtc.AddHours(3);

        if (EventUrl != request.EventUrl)
        {
            EventUrl = request.EventUrl;
            AdbCommand = null;
        }
    }
}
