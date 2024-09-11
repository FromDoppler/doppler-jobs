using System.Linq;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService;
using Doppler.Billing.Job.Database;
using Doppler.Billing.Job.Mappers;
using Doppler.Billing.Job.Settings;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.Billing.Job
{
    public class DopplerBillingUsJob
    {
        private readonly ILogger<DopplerBillingUsJob> _logger;
        
        private readonly IDopplerSapService _dopplerSapService;
        private readonly IDopplerRepository _dopplerRepository;
        private readonly IOptionsMonitor<DopplerBillingUsJobSettings> _dopplerBillingUsJobSettings;
        private readonly IBillingMapper _billingMapper;

        public DopplerBillingUsJob(
            ILogger<DopplerBillingUsJob> logger,
            IDopplerSapService dopplerSapService,
            IDopplerRepository dopplerRepository,
            IOptionsMonitor<DopplerBillingUsJobSettings> dopplerBillingUsJobSettings,
            IBillingMapper billingMapper)
        {
            _logger = logger;
            _dopplerSapService = dopplerSapService;
            _dopplerRepository = dopplerRepository;
            _dopplerBillingUsJobSettings = dopplerBillingUsJobSettings;
            _billingMapper = billingMapper;
        }

        [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
        public void Run() => RunAsync().GetAwaiter().GetResult();

        private async Task RunAsync()
        {
            _logger.LogInformation("Getting data from Doppler database.");
            var billingData = await _dopplerRepository.GetUserBillingInformation(_dopplerBillingUsJobSettings.CurrentValue.StoredProcedures);

            if (billingData.Any())
            {
                _logger.LogInformation("Sending Billing data to Doppler SAP with {billingData} user billing.",
                    billingData.Count);

                var billingRequest = await _billingMapper.MapToListOfBillingRequest(billingData);
                await _dopplerSapService.SendUserBillings(billingRequest);
            }
        }
    }
}
