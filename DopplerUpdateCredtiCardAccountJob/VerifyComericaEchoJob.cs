using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.UpdateCredtiCardAccount.Job.Entities;
using Doppler.UpdateCredtiCardAccount.Job.Services;
using Doppler.UpdateCredtiCardAccount.Job.Settings;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.UpdateCredtiCardAccount.Job;

public class VerifyComericaEchoJob(
    ILogger<VerifyComericaEchoJob> logger,
    ICreditCardService creditCardService,
    IFtpService ftpService,
    IOptionsMonitor<UpdateCredtiCardAccountJobSettings> settings,
    TimeZoneJobConfigurations timeZoneConfig)
{
    [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
    public void Run() => RunAsync().GetAwaiter().GetResult();

    private async Task RunAsync()
    {
        var config = settings.CurrentValue;
        var counterPath = Path.Combine(config.LocalUploadFilePath, DopplerUpdateCredtiCardAccountJob.EchoCounterFileName);

        var iteration = CycleHelper.IncrementCounter(counterPath);
        if (iteration > config.MaxPollingRetries)
        {
            logger.LogError(
                "Echo file polling exceeded {MaxRetries} iterations. Stopping. Manual intervention required.",
                config.MaxPollingRetries);
            RecurringJob.RemoveIfExists(config.VerifyEchoJobIdentifier);
            return;
        }

        logger.LogInformation("Starting echo file verification (iteration {Iteration}/{MaxRetries}).",
            iteration, config.MaxPollingRetries);

        var files = await ftpService.ListFiles(config.RemoteEchoPath);

        var echoFileName = files
            .Where(f => f.StartsWith(config.EchoFilePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (echoFileName == null)
        {
            logger.LogInformation(
                "No echo file found in {RemoteEchoPath}. Will retry.",
                config.RemoteEchoPath);
            return;
        }

        var remoteEchoFilePath = $"{config.RemoteEchoPath.TrimEnd('/')}/{echoFileName}";

        var result = await creditCardService.VerifyComericaRequestDelivery(remoteEchoFilePath);

        switch (result.Status)
        {
            case EchoValidationStatus.NotFound:
                logger.LogInformation("Echo file not found at {Path}. Will retry.", remoteEchoFilePath);
                return;

            case EchoValidationStatus.Success:
                logger.LogInformation("Echo file validated successfully. Scheduling response processing, stopping echo verification.");

                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneConfig.TimeZoneJobs);
                RecurringJob.AddOrUpdate<ProcessComericaResponseJob>(
                    config.ProcessResponseJobIdentifier,
                    job => job.Run(),
                    config.PollingCronExpression,
                    tz);

                RecurringJob.RemoveIfExists(config.VerifyEchoJobIdentifier);
                break;

            case EchoValidationStatus.Failed:
            case EchoValidationStatus.InvalidFormat:
                logger.LogError(
                    "Echo file validation failed with status {Status}: {ErrorMessage}. Stopping polling. Manual intervention required.",
                    result.Status, result.ErrorMessage);
                RecurringJob.RemoveIfExists(config.VerifyEchoJobIdentifier);
                break;
        }
    }
}
