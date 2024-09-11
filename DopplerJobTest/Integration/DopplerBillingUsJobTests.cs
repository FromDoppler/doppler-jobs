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
    public class DopplerBillingUsJobTests
    {

        private readonly Mock<ILogger<DopplerBillingUsJob>> _loggerMock;
        private readonly Mock<IDopplerSapService> _dopplerSapServiceMock;
        private readonly Mock<IDopplerRepository> _dopplerRepositoryMock;
        private readonly Mock<IOptionsMonitor<DopplerBillingUsJobSettings>> _dopplerBillingUsJobSettingsMock;
        private readonly Mock<IBillingMapper> _billingMapperMock;

        public DopplerBillingUsJobTests()
        {
            _loggerMock = new Mock<ILogger<DopplerBillingUsJob>>();
            _dopplerSapServiceMock = new Mock<IDopplerSapService>();
            _dopplerRepositoryMock = new Mock<IDopplerRepository>();
            _dopplerBillingUsJobSettingsMock = new Mock<IOptionsMonitor<DopplerBillingUsJobSettings>>();
            _billingMapperMock = new Mock<IBillingMapper>();
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeNoSendDataToSap_WhenListIsHaveOneCurrencyArs()
        {
            _dopplerBillingUsJobSettingsMock.Setup(s => s.CurrentValue).Returns(new DopplerBillingUsJobSettings());
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<UserBilling>());

            var job = new DopplerBillingUsJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object,
                _dopplerBillingUsJobSettingsMock.Object,
                _billingMapperMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeSendDataToSap_WhenListIsHaveOneUserBillingCreated()
        {
            _dopplerBillingUsJobSettingsMock.Setup(s => s.CurrentValue).Returns(new DopplerBillingUsJobSettings());
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<UserBilling>
                {
                    new UserBilling()
                });

            var job = new DopplerBillingUsJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object,
                _dopplerBillingUsJobSettingsMock.Object,
                _billingMapperMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Billing data to Doppler SAP with 1 user billing.", Times.Once());
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeSendDataToSap_WhenStoredProceduresAreRunCorrectly()
        {
            _dopplerBillingUsJobSettingsMock.Setup(s => s.CurrentValue).Returns(new DopplerBillingUsJobSettings());
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<UserBilling>
                {
                    new UserBilling(),
                    new UserBilling()
                });

            var job = new DopplerBillingUsJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object,
                _dopplerBillingUsJobSettingsMock.Object,
                _billingMapperMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Billing data to Doppler SAP with 2 user billing.", Times.Once());
        }
    }
}
