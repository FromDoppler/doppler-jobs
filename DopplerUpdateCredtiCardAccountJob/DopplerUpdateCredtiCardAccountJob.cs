using System.Threading.Tasks;
using Doppler.UpdateCredtiCardAccount.Job.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Doppler.UpdateCredtiCardAccount.Job;

public class DopplerUpdateCredtiCardAccountJob(
    ILogger<DopplerUpdateCredtiCardAccountJob> logger,
    ICreditCardService creditCardService)
{
    [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
    public void Run() => RunAsync().GetAwaiter().GetResult();

    private async Task RunAsync()
    {
        logger.LogInformation("Starting UpdateCredtiCardAccount job.");

        // await creditCardService.SendCurrentCCDataToComerica();

        logger.LogInformation("UpdateCredtiCardAccount job completed successfully.");
    }
}
