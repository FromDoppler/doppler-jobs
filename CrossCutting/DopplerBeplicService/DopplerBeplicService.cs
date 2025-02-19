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
    public class DopplerBeplicService : IDopplerBeplicService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<DopplerBeplicService> logger;
        private readonly IJwtTokenGenerator jwtTokenGenerator;
        private readonly DopplerBeplicServiceConfiguration dopplerBeplicServiceConfiguration;

        public DopplerBeplicService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<DopplerBeplicServiceConfiguration> dopplerBeplicServiceConfiguration,
            ILogger<DopplerBeplicService> logger,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            this.dopplerBeplicServiceConfiguration = dopplerBeplicServiceConfiguration.CurrentValue;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<int> GetConversationsByUserIdAndPeriodAsync(int userId, DateTime dateFrom, DateTime dateTo)
        {
            var uri = $"{string.Format(dopplerBeplicServiceConfiguration.GetConversationsEndpoint, userId)}?dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}";
            logger.LogInformation("Building http request with url {uri}.", uri);

            var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = new HttpMethod("GET")
            };

            var httpResponse = new HttpResponseMessage();

            try
            {
                var jwtToken = jwtTokenGenerator.CreateJwtToken();
                logger.LogInformation("Sending request to Doppler Beplic Api.");
                var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                httpResponse = await client.SendAsync(httpRequest).ConfigureAwait(false);
                var jsonResult = await httpResponse.Content.ReadAsStringAsync();

                int result;
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
                logger.LogError(e, "Error occurred trying to send information to Doppler Beplic Api return http {code}.", httpResponse.StatusCode);
                throw;
            }
        }
    }
}
