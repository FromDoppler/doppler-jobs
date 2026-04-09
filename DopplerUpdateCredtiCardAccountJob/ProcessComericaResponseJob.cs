using System;
using System.IO;
using System.Linq;
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

        var files = await ftpService.ListFiles(config.RemoteResponsePath);

        var responseFileName = files
            .Where(f => f.StartsWith(config.ResponseFilePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (responseFileName == null)
        {
            logger.LogInformation(
                "No response file found in {RemoteResponsePath}. Will retry.",
                config.RemoteResponsePath);
            return;
        }

        var remoteResponseFilePath = $"{config.RemoteResponsePath.TrimEnd('/')}/{responseFileName}";

        logger.LogInformation("Downloading response file: {RemoteResponseFilePath}.", remoteResponseFilePath);
        var content = await ftpService.DownloadFileContent(remoteResponseFilePath);

        var results = creditCardService.ProcessAccountUpdaterResponse(content);

        logger.LogInformation(
            "Response file processed successfully. {Count} actionable records found. Stopping response processing.",
            results.Count);

        RecurringJob.RemoveIfExists(config.ProcessResponseJobIdentifier);
    }
}
