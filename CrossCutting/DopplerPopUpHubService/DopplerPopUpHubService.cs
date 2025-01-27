using CrossCutting.Authorization;
using CrossCutting.DopplerPopUpHubService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CrossCutting.DopplerPopUpHubService
{
    public class DopplerPopUpHubService : IDopplerPopUpHubService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<DopplerPopUpHubService> logger;
        private readonly IJwtTokenGenerator jwtTokenGenerator;
        private readonly DopplerPopUpHubServiceConfiguration dopplerPopUpHubServiceConfiguration;

        public DopplerPopUpHubService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<DopplerPopUpHubServiceConfiguration> dopplerPopUpHubServiceConfiguration,
            ILogger<DopplerPopUpHubService> logger,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            this.dopplerPopUpHubServiceConfiguration = dopplerPopUpHubServiceConfiguration.CurrentValue;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<int> GetImpressionsByUserIdAndPeriodAsync(int userId, string email, DateTime dateFrom, DateTime dateTo)
        {
            var uri = $"{dopplerPopUpHubServiceConfiguration.GetImpressionsEndpoint}?accountName={email}&dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}";
            logger.LogInformation("Building http request with url {uri}.", uri);

            var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = new HttpMethod("GET")
            };

            var result = 0;
            var httpResponse = new HttpResponseMessage();

            try
            {
                var jwtToken = jwtTokenGenerator.CreateJwtToken();
                logger.LogInformation("Sending request to Doppler PopUpHub Api.");
                var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                httpResponse = await client.SendAsync(httpRequest).ConfigureAwait(false);
                var jsonResult = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    result = JsonConvert.DeserializeObject<int>(jsonResult);
                }
                else
                {
                    result = 0;
                }

                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred trying to send information to Doppler PopUpHub Api return http {code}.", httpResponse.StatusCode);
                throw;
            }
        }
    }
}
