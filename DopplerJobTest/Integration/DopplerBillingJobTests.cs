using CrossCutting.DopplerSapService;
using Doppler.Billing.Job;
using Doppler.Billing.Job.Database;
using Doppler.Billing.Job.Entities;
using Doppler.Billing.Job.Mappers;
using Doppler.Billing.Job.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Doppler.Jobs.Test.Integration
{
    public class DopplerBillingJobTests
    {
        
        private readonly Mock<ILogger<DopplerBillingJob>> _loggerMock;
        private readonly Mock<IDopplerSapService> _dopplerSapServiceMock;
        private readonly Mock<IDopplerRepository> _dopplerRepositoryMock;
        private readonly Mock<IOptionsMonitor<DopplerBillingJobSettings>> _dopplerBillingJobSettingsMock;
        private readonly Mock<IBillingMapper> _billingMapperMock;

        public DopplerBillingJobTests()
        {
            _loggerMock = new Mock<ILogger<DopplerBillingJob>>();
            _dopplerSapServiceMock = new Mock<IDopplerSapService>();
            _dopplerRepositoryMock = new Mock<IDopplerRepository>();
            _dopplerBillingJobSettingsMock = new Mock<IOptionsMonitor<DopplerBillingJobSettings>>();
            _billingMapperMock = new Mock<IBillingMapper>();
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeNoSendDataToSap_WhenListIsHaveOneCurrencyArs()
        {
            _dopplerBillingJobSettingsMock.Setup(s => s.CurrentValue).Returns(new DopplerBillingJobSettings());
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<UserBilling>());

            var job = new DopplerBillingJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object,
                _dopplerBillingJobSettingsMock.Object,
                _billingMapperMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeSendDataToSap_WhenListIsHaveOneUserBillingCreated()
        {
            _dopplerBillingJobSettingsMock.Setup(s => s.CurrentValue).Returns(new DopplerBillingJobSettings());
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<UserBilling>
                {
                    new UserBilling()
                });

            var job = new DopplerBillingJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object,
                _dopplerBillingJobSettingsMock.Object,
                _billingMapperMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Billing data to Doppler SAP with 1 user billing.", Times.Once());
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeSendDataToSap_WhenStoredProceduresAreRunCorrectly()
        {
            _dopplerBillingJobSettingsMock.Setup(s => s.CurrentValue).Returns(new DopplerBillingJobSettings());
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<UserBilling>
                {
                    new UserBilling(),
                    new UserBilling()
                });

            var job = new DopplerBillingJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object,
                _dopplerBillingJobSettingsMock.Object,
                _billingMapperMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Billing data to Doppler SAP with 2 user billing.", Times.Once());
        }
    }
}
