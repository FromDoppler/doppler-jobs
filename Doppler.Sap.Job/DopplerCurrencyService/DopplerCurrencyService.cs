﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.Sap.Job.Service.Dtos;
using Doppler.Sap.Job.Service.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Doppler.Sap.Job.Service.DopplerCurrencyService
{
    public class DopplerCurrencyService : ExternalService, IDopplerCurrencyService
    {
        private readonly DopplerCurrencySettings _dopplerCurrencySettings;
        private readonly TimeZoneJobConfigurations _jobConfig;

        public DopplerCurrencyService(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings httpClientPoliciesSettings,
            DopplerCurrencySettings dopplerCurrencySettings,
            ILogger<ExternalService> logger,
            TimeZoneJobConfigurations jobConfig) : 
            base(httpClientFactory.CreateClient(httpClientPoliciesSettings.ClientName), logger)
        {
            _dopplerCurrencySettings = dopplerCurrencySettings;
            _jobConfig = jobConfig;
        }

        public async Task<IList<CurrencyDto>> GetCurrencyByCode()
        {
            var cstZone = TimeZoneInfo.FindSystemTimeZoneById(_jobConfig.TimeZoneJobs);
            var cstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);
            
            var returnList = new List<CurrencyDto>();
            
            foreach (var currencyCode in _dopplerCurrencySettings.CurrencyCodeList)
            {
                var uri = new Uri(_dopplerCurrencySettings.Url + $"{currencyCode}/{cstTime.Year}-{cstTime.Month}-{cstTime.Day}");

                Logger.LogInformation($"Building http request with url {uri}");
                var httpRequest = new HttpRequestMessage
                {
                    RequestUri = uri,
                    Method = new HttpMethod("GET")
                };

                try
                {
                    Logger.LogInformation("Sending request to Doppler Currency Api.");
                    var httpResponse = await HttpClient.SendAsync(httpRequest).ConfigureAwait(false);

                    if (!httpResponse.IsSuccessStatusCode)
                        continue;

                    var json = await httpResponse.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<CurrencyDto>(json);
                    result.Entity.CurrencyCode = currencyCode;

                    returnList.Add(result);
                }
                catch (Exception e)
                {
                    Logger.LogError(e,$"Error GetCurrency for {currencyCode}.");
                    throw;
                }
            }

            return returnList;
        }
    }
}
