using CrossCutting.DopplerBillingUserService;
using CrossCutting.DopplerPopUpHubService;
using CrossCutting.Extensions;
using Doppler.CancelAccountWithScheduleCancellation.Job.Database;
using Doppler.CancelAccountWithScheduleCancellation.Job.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Doppler.CancelAccountWithScheduleCancellation.Job
{
    public class DopplerCancelAccountWithScheduleCancellationJob
    {
        private readonly ILogger<DopplerCancelAccountWithScheduleCancellationJob> logger;
        private readonly IDopplerRepository dopplerRepository;
        private readonly IDopplerBillingUserService dopplerBillingUserService;

        public DopplerCancelAccountWithScheduleCancellationJob(
            ILogger<DopplerCancelAccountWithScheduleCancellationJob> logger,
            IDopplerRepository dopplerRepository,
            IDopplerBillingUserService dopplerBillingUserService)
        {
            this.logger = logger;
            this.dopplerRepository = dopplerRepository;
            this.dopplerBillingUserService = dopplerBillingUserService;
        }

        [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
        public void Run() => RunAsync().GetAwaiter().GetResult();

        private async Task RunAsync()
        {
            logger.LogInformation("Getting data from Doppler database.");
            var usersToCancel = await dopplerRepository.GetUserWithScheduleCancellationAsync();

            foreach (var user in usersToCancel)
            {
                var accountCancellationReasonDescription = string.Empty;
                var accountCancellationReasonFromDB = await dopplerRepository.GetAccountCancellationReasonByIdAsync(user.AccountCancellationReasonId ?? 0);
                if (accountCancellationReasonFromDB != null)
                {
                    accountCancellationReasonDescription = accountCancellationReasonFromDB.Description;
                }
                    
                var response = await dopplerBillingUserService.CancelAccountAsync(user.Email, accountCancellationReasonDescription);

                if (response)
                {
                    await dopplerRepository.UnsetSetScheduleCancellationAsync(user.UserId);
                }
            }
        }
    }
}
