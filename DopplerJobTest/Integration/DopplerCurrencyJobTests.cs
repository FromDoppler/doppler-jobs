﻿using System;
using System.Collections.Generic;
using System.Text;
using CrossCutting.DopplerSapService;
using CrossCutting.DopplerSapService.Entities;
using Doppler.Currency.Job;
using Doppler.Currency.Job.DopplerCurrencyService;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Doppler.Jobs.Test.Integration
{
    public class DopplerCurrencyJobTests
    {
        private readonly Mock<IDopplerCurrencyService> _dopplerCurrencyServiceMock;
        private readonly Mock<ILogger<DopplerCurrencyJob>> _loggerMock;
        private readonly Mock<IDopplerSapService> _dopplerSapServiceMock;

        public DopplerCurrencyJobTests()
        {
            _dopplerCurrencyServiceMock = new Mock<IDopplerCurrencyService>();
            _loggerMock = new Mock<ILogger<DopplerCurrencyJob>>();
            _dopplerSapServiceMock = new Mock<IDopplerSapService>();
        }

        [Fact]
        public void DopplerSapJob_ShouldBeNoSendDataToSap_WhenListIsEmpty()
        {
            _dopplerCurrencyServiceMock.Setup(x => x.GetCurrencyByCode())
                .ReturnsAsync(new List<CurrencyResponse>());

            var job = new DopplerCurrencyJob(
                _loggerMock.Object,
                _dopplerCurrencyServiceMock.Object,
                _dopplerSapServiceMock.Object);

            job.Run();

            Assert.True(true);

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting currency per each code enabled.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending data to Doppler SAP system.", Times.Never());
        }
    }
}
