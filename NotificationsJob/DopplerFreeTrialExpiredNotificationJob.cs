using Doppler.Notifications.Job.Database;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Notifications.Job
{
    public class DopplerFreeTrialExpiredNotificationJob
    {
        private readonly ILogger<DopplerFreeTrialExpiredNotificationJob> _logger;
        private readonly IDopplerRepository _dopplerRepository;

        public DopplerFreeTrialExpiredNotificationJob(
            ILogger<DopplerFreeTrialExpiredNotificationJob> logger,
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

            var userNotifications = await _dopplerRepository.GetUserWithTrialExpired();

            if (userNotifications.Any())
            {
                //TODO: Integrate with Email service to send the notifications
                _logger.LogInformation("Sending Notifications with {userNotifications} users.", userNotifications.Count);
            }
        }
    }
}
