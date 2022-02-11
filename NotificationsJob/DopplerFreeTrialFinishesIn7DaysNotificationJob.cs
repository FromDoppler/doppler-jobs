using CrossCutting.EmailSenderService;
using Doppler.Notifications.Job.Database;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Notifications.Job
{
    public class DopplerFreeTrialFinishesIn7DaysNotificationJob
    {
        private readonly ILogger<DopplerFreeTrialFinishesIn7DaysNotificationJob> _logger;
        private readonly IDopplerRepository _dopplerRepository;
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly IEmailSender _emailSender;

        private const int days = 7;

        public DopplerFreeTrialFinishesIn7DaysNotificationJob(
            ILogger<DopplerFreeTrialFinishesIn7DaysNotificationJob> logger,
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

            var userNotifications = await _dopplerRepository.GetUserWithTrialExpiresInDays(days);

            if (userNotifications.Any())
            {
                foreach (var userNotification in userNotifications)
                {
                    var template = _emailSettings.Value.FreeTrialExpiresIn7DaysNotificationsTemplateId[userNotification.Language ?? "en"];

                    await _emailSender.SafeSendWithTemplateAsync(
                            templateId: template,
                            templateModel: new
                            {
                                urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                                trialExpirationDate = userNotification.TrialExpirationDate.AddDays(-1).ToString("dd/MM/yyyy")
                            },
                            to: new[] { userNotification.Email });
                }

                _logger.LogInformation("Sending Free Trial Expires in 7 day Notifications to {userNotifications} users.", userNotifications.Count);
            }
        }
    }
}
