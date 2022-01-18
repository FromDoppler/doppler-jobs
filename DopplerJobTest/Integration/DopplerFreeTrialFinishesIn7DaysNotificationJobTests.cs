using CrossCutting.Notificacion.Entities;
using Doppler.Notifications.Job;
using Doppler.Notifications.Job.Database;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Doppler.Jobs.Test.Integration
{
    public class DopplerFreeTrialFinishesIn7DaysNotificationJobTests
    {

        private readonly Mock<ILogger<DopplerFreeTrialFinishesIn7DaysNotificationJob>> _loggerMock;
        private readonly Mock<IDopplerRepository> _dopplerRepositoryMock;

        public DopplerFreeTrialFinishesIn7DaysNotificationJobTests()
        {
            _loggerMock = new Mock<ILogger<DopplerFreeTrialFinishesIn7DaysNotificationJob>>();
            _dopplerRepositoryMock = new Mock<IDopplerRepository>();
        }

        [Fact]
        public void DopplerFreeTrialFinishesIn7DaysNotificationJob_ShouldBeSendNotifications_WhenListIsHaveOneUserNotifications()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserWithTrialExpiresInDays(It.IsAny<int>()))
                .ReturnsAsync(new List<UserNotification>() { new UserNotification { Email = "test@test.com" } });

            var job = new DopplerFreeTrialFinishesIn7DaysNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Notifications with 1 users.", Times.Once());
        }

        [Fact]
        public void DopplerFreeTrialFinishesIn7DaysNotificationJob_ShouldNotBeSendNotifications_WhenListIsEmpty()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserWithTrialExpiresInDays(It.IsAny<int>()))
                .ReturnsAsync(new List<UserNotification>());

            var job = new DopplerFreeTrialFinishesIn7DaysNotificationJob(
                _loggerMock.Object,
                _dopplerRepositoryMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Notifications with 1 users.", Times.Never());
        }
    }
}
