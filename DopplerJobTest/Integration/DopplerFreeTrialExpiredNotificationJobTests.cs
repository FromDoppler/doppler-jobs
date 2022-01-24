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
    public class DopplerFreeTrialExpiredNotificationJobTests
    {

        private readonly Mock<ILogger<DopplerFreeTrialExpiredNotificationJob>> _loggerMock;
        private readonly Mock<IDopplerRepository> _dopplerRepositoryMock;

        public DopplerFreeTrialExpiredNotificationJobTests()
        {
            _loggerMock = new Mock<ILogger<DopplerFreeTrialExpiredNotificationJob>>();
            _dopplerRepositoryMock = new Mock<IDopplerRepository>();
        }

        [Fact]
        public void DopplerFreeTrialExpiredNotificationJob_ShouldBeSendNotifications_WhenListIsHaveOneUserNotifications()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserWithTrialExpired())
                .ReturnsAsync(new List<UserNotification>() { new UserNotification { Email = "test@test.com" } });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var job = new DopplerFreeTrialExpiredNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object,
                GetEmailNotificationsConfigurationMock().Object,
                emailSenderMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Free Trial Expired Notifications to 1 users.", Times.Once());
        }

        [Fact]
        public void DopplerFreeTrialExpiredNotificationJob_ShouldNotBeSendNotifications_WhenListIsEmpty()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserWithTrialExpired())
                .ReturnsAsync(new List<UserNotification>());

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var job = new DopplerFreeTrialExpiredNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object,
                GetEmailNotificationsConfigurationMock().Object,
                emailSenderMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Free Trial Expired Notifications to 1 users.", Times.Never());
        }

        private static Mock<IOptions<EmailNotificationsConfiguration>> GetEmailNotificationsConfigurationMock()
        {
            var emailNotificationsConfigurationMock = new Mock<IOptions<EmailNotificationsConfiguration>>();
            emailNotificationsConfigurationMock.Setup(x => x.Value)
                .Returns(new EmailNotificationsConfiguration
                {
                    FreeTrialExpiredNotificationsTemplateId = new Dictionary<string, string>
                    {
                        { "es", "TEMPLATE_ES" },
                        { "en", "TEMPLATE_EN" }
                    }
                });

            return emailNotificationsConfigurationMock;
        }
    }
}
