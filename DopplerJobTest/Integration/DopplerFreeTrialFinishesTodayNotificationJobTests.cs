using CrossCutting.EmailSenderService;
using CrossCutting.Notificacion.Entities;
using Doppler.Notifications.Job;
using Doppler.Notifications.Job.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Doppler.Jobs.Test.Integration
{
    public class DopplerFreeTrialFinishesTodayNotificationJobTests
    {

        private readonly Mock<ILogger<DopplerFreeTrialFinishesTodayNotificationJob>> _loggerMock;
        private readonly Mock<IDopplerRepository> _dopplerRepositoryMock;

        public DopplerFreeTrialFinishesTodayNotificationJobTests()
        {
            _loggerMock = new Mock<ILogger<DopplerFreeTrialFinishesTodayNotificationJob>>();
            _dopplerRepositoryMock = new Mock<IDopplerRepository>();
        }

        [Fact]
        public void DopplerFreeTrialFinishesTodayNotificationJob_ShouldBeSendNotifications_WhenListIsHaveOneUserNotifications()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserWithTrialExpiresInDays(It.IsAny<int>()))
                .ReturnsAsync(new List<UserNotification>() { new UserNotification { Email = "test@test.com", TrialExpirationDate = System.DateTime.Today } });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var job = new DopplerFreeTrialFinishesTodayNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object,
                GetEmailNotificationsConfigurationMock().Object,
                emailSenderMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Free Trial Expires Today Notifications to 1 users.", Times.Once());
        }

        [Fact]
        public void DopplerFreeTrialFinishesTodayNotificationJob_ShouldNotBeSendNotifications_WhenListIsEmpty()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserWithTrialExpiresInDays(It.IsAny<int>()))
                .ReturnsAsync(new List<UserNotification>());

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var job = new DopplerFreeTrialFinishesTodayNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object,
                GetEmailNotificationsConfigurationMock().Object,
                emailSenderMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Free Trial Expires Today Notifications to 1 users.", Times.Never());
        }

        private static Mock<IOptions<EmailNotificationsConfiguration>> GetEmailNotificationsConfigurationMock()
        {
            var emailNotificationsConfigurationMock = new Mock<IOptions<EmailNotificationsConfiguration>>();
            emailNotificationsConfigurationMock.Setup(x => x.Value)
                .Returns(new EmailNotificationsConfiguration
                {
                    FreeTrialExpiresTodayNotificationsTemplateId = new Dictionary<string, string>
                    {
                        { "es", "TEMPLATE_ES" },
                        { "en", "TEMPLATE_EN" }
                    }
                });

            return emailNotificationsConfigurationMock;
        }
    }
}
