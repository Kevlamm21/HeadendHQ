namespace HeadendHQ.Core.Shared;

// SQLite returns DateTimes as Unspecified, which ToLocalTime treats as already local — hence SpecifyKind.
public static class LocalDay
{
    public static bool IsToday(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().Date == DateTime.Now.Date;

    public static bool IsTodayOrEarlier(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().Date <= DateTime.Now.Date;

    public static (DateTime FromUtc, DateTime ToUtc) UtcWindow(int leadDays = 0)
    {
        var localStart = DateTime.Now.Date;
        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZoneInfo.Local),
            TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1 + leadDays), TimeZoneInfo.Local));
    }
}
