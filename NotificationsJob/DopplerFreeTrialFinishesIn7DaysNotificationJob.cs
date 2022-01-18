using Doppler.Notifications.Job.Database;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Notifications.Job
{
    public class DopplerFreeTrialFinishesIn7DaysNotificationJob
    {
        private readonly ILogger<DopplerFreeTrialFinishesIn7DaysNotificationJob> _logger;
        private readonly IDopplerRepository _dopplerRepository;

        private const int days = 7;

        public DopplerFreeTrialFinishesIn7DaysNotificationJob(
            ILogger<DopplerFreeTrialFinishesIn7DaysNotificationJob> logger,
            IDopplerRepository dopplerRepository)
        {
            _logger = logger;
            _dopplerRepository = dopplerRepository; ;
        }

        [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
        public void Run() => RunAsync().GetAwaiter().GetResult();

        private async Task RunAsync()
        {
            _logger.LogInformation("Getting data from Doppler database.");

            var userNotifications = await _dopplerRepository.GetUserWithTrialExpiresInDays(days);

            if (userNotifications.Any())
            {
                _logger.LogInformation("Sending Notifications with {userNotifications} users.", userNotifications.Count);
            }
        }
    }
}
