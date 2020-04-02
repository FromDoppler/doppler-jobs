﻿using System.Net.Http;
using CrossCutting;
using Doppler.Sap.Job.Service.DopplerCurrencyService;
using Doppler.Sap.Job.Service.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Doppler.Jobs.Test
{
    public static class CreateSutCurrencyServiceTests
    {
        public static DopplerCurrencyService CreateSut(
            IHttpClientFactory httpClientFactory = null,
            HttpClientPoliciesSettings httpClientPoliciesSettings = null,
            DopplerCurrencyServiceSettings dopplerCurrencySettings = null,
            ILogger<DopplerCurrencyService> loggerCurrencyService = null,
            TimeZoneJobConfigurations timeZoneJobConfigurations = null)
        {

            return new DopplerCurrencyService(
                httpClientFactory,
                httpClientPoliciesSettings,
                dopplerCurrencySettings,
                loggerCurrencyService ?? Mock.Of<ILogger<DopplerCurrencyService>>(),
                timeZoneJobConfigurations ?? new TimeZoneJobConfigurations
                {
                    TimeZoneJobs = "Argentina Standard Time"
                });
        }
    }
}