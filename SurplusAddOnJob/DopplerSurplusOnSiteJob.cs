using CrossCutting;
using CrossCutting.DopplerPopUpHubService;
using Doppler.SurplusAddOn.Job.Database;
using Doppler.SurplusAddOn.Job.Enums;
using Doppler.SurplusAddOn.Job.Utils;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Doppler.SurplusAddOn.Job
{
    public class DopplerSurplusOnSiteJob
    {
        private readonly ILogger<DopplerSurplusOnSiteJob> logger;
        private readonly IDopplerPopUpHubService dopplerPopUpHubService;
        private readonly IDopplerRepository dopplerRepository;
        private readonly IConfiguration configuration;
        private readonly IDateTimeProvider dateTimeProvider;

        public DopplerSurplusOnSiteJob(
            ILogger<DopplerSurplusOnSiteJob> logger, 
            IDopplerPopUpHubService dopplerPopUpHubService,
            IDopplerRepository dopplerRepository,
            IConfiguration configuration,
            IDateTimeProvider dateTimeProvider)
        {
            this.logger = logger;
            this.dopplerPopUpHubService = dopplerPopUpHubService;
            this.dopplerRepository = dopplerRepository;
            this.configuration = configuration;
            this.dateTimeProvider = dateTimeProvider;
        }


        [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
        public void Run() => RunAsync().GetAwaiter().GetResult();

        private async Task RunAsync()
        {
            logger.LogInformation("Getting data from Doppler OnSite api.");

            var usersWithAddOn = await dopplerRepository.GetUsersWithActiveAddOnByAddOnTypeAsync((int)AddOnTypeEnum.OnSite);
            var currentDate = dateTimeProvider.GetDateByTimezoneId(dateTimeProvider.UtcNow, configuration["TimeZoneJobs"]);

            var dateFrom = new DateTime(currentDate.Year, currentDate.Month, 1);
            var dateTo = dateFrom.AddMonths(1).AddDays(-1);

            foreach ( var user in usersWithAddOn)
            {
                var response = await dopplerPopUpHubService.GetImpressionsByUserIdAndPeriodAsync(user.UserId, user.Email, dateFrom, dateTo);

                logger.LogInformation($"Current impressions: {response} from Doppler OnSite api.");

                if (response > 0)
                {
                    if (user.Qty < response)
                    {
                        var surplus = response - user.Qty;
                        CultureInfo ci = new("en-US");
                        var period = dateFrom.ToString("yyyy-MMM", ci);

                        var surplusAddOn = await dopplerRepository.GetByUserIdAddOnTypeIdAndPeridoAsync(user.UserId, (int)AddOnTypeEnum.OnSite, period);

                        if (surplusAddOn == null)
                        {
                            await dopplerRepository.InsertSurplusAddOnAsync(user.UserId, (int)AddOnTypeEnum.OnSite, currentDate, period, surplus, user.AdditionalPrice, surplus * user.AdditionalPrice);
                        }
                        else
                        {
                            await dopplerRepository.UpdateSurplusAddOnAsync(user.UserId, (int)AddOnTypeEnum.OnSite, currentDate, period, surplus, user.AdditionalPrice, surplus * user.AdditionalPrice);
                        }
                    }
                }
            }
        }
    }
}
