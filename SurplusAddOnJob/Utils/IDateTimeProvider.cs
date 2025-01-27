namespace Doppler.SurplusAddOn.Job.Utils
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }

        DateTime GetDateByTimezoneId(DateTime date, string timezoneId);
    }
}
