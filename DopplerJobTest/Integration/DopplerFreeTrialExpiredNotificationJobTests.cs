using CrossCutting.Notificacion.Entities;
using Doppler.Notifications.Job;
using Doppler.Notifications.Job.Database;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
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

            var job = new DopplerFreeTrialExpiredNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Notifications with 1 users.", Times.Once());
        }

        [Fact]
        public void DopplerFreeTrialExpiredNotificationJob_ShouldNotBeSendNotifications_WhenListIsEmpty()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserWithTrialExpired())
                .ReturnsAsync(new List<UserNotification>());

            var job = new DopplerFreeTrialExpiredNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Notifications with 1 users.", Times.Never());
        }
    }
}
