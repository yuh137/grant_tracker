namespace GrantTracker.Api;

/// <summary>
/// The framework <see cref="TimeProvider"/> with the office's time zone instead of the machine's.
/// "Today" for BR-2 and BR-6 is the office's calendar day, which stops a server running in UTC (a
/// container, a cloud region) from reporting tasks overdue a few hours early or late.
/// </summary>
public sealed class OfficeTimeProvider(TimeZoneInfo officeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => officeZone;
}

public static class TimeProviderExtensions
{
    /// <summary>The office's current calendar date. The only place the app turns an instant into a day.</summary>
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
