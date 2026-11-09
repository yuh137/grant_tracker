namespace GrantTracker.Api;

public class OfficeTimeProvider(TimeZoneInfo officeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => officeZone;
}

public static class TimeProviderExtensions
{
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
