using CrossCutting.EmailSenderService;
using Doppler.Notifications.Job.Database;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Notifications.Job
{
    public class DopplerFreeTrialExpiredNotificationJob
    {
        private readonly ILogger<DopplerFreeTrialExpiredNotificationJob> _logger;
        private readonly IDopplerRepository _dopplerRepository;
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly IEmailSender _emailSender;

        public DopplerFreeTrialExpiredNotificationJob(
            ILogger<DopplerFreeTrialExpiredNotificationJob> logger,
            IDopplerRepository dopplerRepository,
            IOptions<EmailNotificationsConfiguration> emailSettings,
            IEmailSender emailSender)
        {
            _logger = logger;
            _dopplerRepository = dopplerRepository;
            _emailSettings = emailSettings;
            _emailSender = emailSender;
        }

        [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
        public void Run() => RunAsync().GetAwaiter().GetResult();

        private async Task RunAsync()
        {
            _logger.LogInformation("Getting data from Doppler database.");

            var userNotifications = await _dopplerRepository.GetUserWithTrialExpired();

            if (userNotifications.Any())
            {
                foreach (var userNotification in userNotifications)
                {
                    var template = _emailSettings.Value.FreeTrialExpiredNotificationsTemplateId[userNotification.Language ?? "en"];

                    await _emailSender.SafeSendWithTemplateAsync(
                            templateId: template,
                            templateModel: new
                            {
                                urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                                trialExpirationDate = userNotification.TrialExpirationDate.ToString("dd/MM/yyyy")
                            },
                            to: new[] { userNotification.Email });
                }

                _logger.LogInformation("Sending Free Trial Expired Notifications to {userNotifications} users.", userNotifications.Count);
            }
        }
    }
}
