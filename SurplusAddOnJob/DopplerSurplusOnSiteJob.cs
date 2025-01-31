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

            var usersWithOnSite = await dopplerRepository.GetUsersWithActiveOnsitePlanAsync();
            var currentDate = dateTimeProvider.GetDateByTimezoneId(dateTimeProvider.UtcNow, configuration["TimeZoneJobs"]);

            var dateFrom = new DateTime(currentDate.Year, currentDate.Month, 1);
            var dateTo = dateFrom.AddMonths(1).AddDays(-1);

            foreach ( var user in usersWithOnSite )
            {
                var response = await dopplerPopUpHubService.GetImpressionsByUserIdAndPeriodAsync(user.UserId, user.Email, dateFrom, dateTo);

                logger.LogInformation($"Current impressions: {response} from Doppler OnSite api.");

                if (response > 0)
                {
                    var activeOnSitePlan = await dopplerRepository.GetActiveOnsitePlanByUserIdAsync(user.UserId);
                    if (activeOnSitePlan != null)
                    {
                        if (activeOnSitePlan.PrintQty < response)
                        {
                            var surplus = response - activeOnSitePlan.PrintQty;
                            CultureInfo ci = new CultureInfo("en-US");
                            var period = dateFrom.ToString("yyyy-MMM", ci);

                            await dopplerRepository.InsertSurplusAddOnAsync(user.UserId, (int)AddOnTypeEnum.OnSite, currentDate, period, surplus, activeOnSitePlan.AdditionalPrint, surplus * activeOnSitePlan.AdditionalPrint);
                        }
                    }
                }
            }
        }
    }
}
