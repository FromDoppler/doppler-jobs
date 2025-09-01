using CrossCutting.Authorization;
using CrossCutting.DopplerBillingUserService.Requests;
using CrossCutting.DopplerPopUpHubService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.DopplerBillingUserService
{
    public class DopplerBillingUserService : IDopplerBillingUserService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<DopplerBillingUserService> logger;
        private readonly IJwtTokenGenerator jwtTokenGenerator;
        private readonly DopplerBillingUserServiceConfiguration dopplerBillingUserServiceConfiguration;
        private readonly JsonSerializerSettings serializationSettings;

        public DopplerBillingUserService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<DopplerBillingUserServiceConfiguration> dopplerBillingUserServiceConfiguration,
            ILogger<DopplerBillingUserService> logger,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            this.dopplerBillingUserServiceConfiguration = dopplerBillingUserServiceConfiguration.CurrentValue;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.jwtTokenGenerator = jwtTokenGenerator;

            serializationSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter()
                }
            };
        }

        public async Task<bool> CancelAccountAsync(string accountName, string cancellationReason)
        {
            var uri = $"{string.Format(dopplerBillingUserServiceConfiguration.CancelAccountEndpoint, accountName)}";
            logger.LogInformation("Building http request with url {uri}.", uri);

            var httpResponse = new HttpResponseMessage();

            try
            {
                var jwtToken = jwtTokenGenerator.CreateJwtToken();
                logger.LogInformation("Sending request to DopplerBillingUserApi.");

                var httpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(uri),
                    Method = new HttpMethod("POST")
                };

                var data = new CancelAccountRequest
                {
                    CancellationReason = cancellationReason,
                    ContactSchedule = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    Phone = string.Empty
                };

                var requestContent = SafeJsonConvert.SerializeObject(data, serializationSettings);
                httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
                httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

                var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                try
                {
                    logger.LogInformation("Sending request to Doppler SAP Api.");
                    httpResponse = await client.SendAsync(httpRequest).ConfigureAwait(false);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var jsonResult = await httpResponse.Content.ReadAsStringAsync();
                        logger.LogError(jsonResult);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error occurred trying to send information to SAP return http {code}.", httpResponse.StatusCode);
                    throw;
                }

                return httpResponse.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred trying to send information to Doppler Beplic Api return http {code}.", httpResponse.StatusCode);
                throw;
            }
        }
    }
}
