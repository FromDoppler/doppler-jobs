using System.IO;
using System.Threading.Tasks;
using Doppler.UpdateCredtiCardAccount.Job.Services;
using Doppler.UpdateCredtiCardAccount.Job.Settings;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.UpdateCredtiCardAccount.Job;

public class ProcessComericaResponseJob(
    ILogger<ProcessComericaResponseJob> logger,
    ICreditCardService creditCardService,
    IFtpService ftpService,
    IOptionsMonitor<UpdateCredtiCardAccountJobSettings> settings)
{
    [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
    public void Run() => RunAsync().GetAwaiter().GetResult();

    private async Task RunAsync()
    {
        var config = settings.CurrentValue;
        var counterPath = Path.Combine(config.LocalUploadFilePath, DopplerUpdateCredtiCardAccountJob.ResponseCounterFileName);

        var iteration = CycleHelper.IncrementCounter(counterPath);
        if (iteration > config.MaxPollingRetries)
        {
            logger.LogError(
                "Response file polling exceeded {MaxRetries} iterations. Stopping. Manual intervention required.",
                config.MaxPollingRetries);
            RecurringJob.RemoveIfExists(config.ProcessResponseJobIdentifier);
            return;
        }

        logger.LogInformation("Starting response file processing (iteration {Iteration}/{MaxRetries}).",
            iteration, config.MaxPollingRetries);

        logger.LogInformation("Searching for response file '{Prefix}*' in {Path}.",
            config.ResponseFileName, config.RemoteResponsePath);

        var content = await ftpService.DownloadFileContentByPrefix(config.RemoteResponsePath, $"{config.ResponseFileName}_");
        if (content == null)
        {
            logger.LogInformation("Response file '{Prefix}*' not found yet in {Path}. Will retry.",
                config.ResponseFileName, config.RemoteResponsePath);
            return;
        }

        var results = creditCardService.ProcessAccountUpdaterResponse(content);

        logger.LogInformation(
            "Response file processed successfully. {Count} actionable records found. Stopping response processing.",
            results.Count);

        RecurringJob.RemoveIfExists(config.ProcessResponseJobIdentifier);
    }
}
