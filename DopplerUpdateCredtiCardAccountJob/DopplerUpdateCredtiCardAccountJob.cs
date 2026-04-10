using System;
using System.IO;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.UpdateCredtiCardAccount.Job.Services;
using Doppler.UpdateCredtiCardAccount.Job.Settings;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.UpdateCredtiCardAccount.Job;

public class DopplerUpdateCredtiCardAccountJob(
    ILogger<DopplerUpdateCredtiCardAccountJob> logger,
    ICreditCardService creditCardService,
    IOptionsMonitor<UpdateCredtiCardAccountJobSettings> settings,
    TimeZoneJobConfigurations timeZoneConfig)
{
    internal const string EchoCounterFileName = "echo_poll_count.txt";
    internal const string ResponseCounterFileName = "response_poll_count.txt";

    [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
    public void Run() => RunAsync().GetAwaiter().GetResult();

    private async Task RunAsync()
    {
        var config = settings.CurrentValue;

        logger.LogInformation("Starting UpdateCredtiCardAccount job.");

        RecurringJob.RemoveIfExists(config.VerifyEchoJobIdentifier);
        RecurringJob.RemoveIfExists(config.ProcessResponseJobIdentifier);

        CycleHelper.ResetCounter(Path.Combine(config.LocalUploadFilePath, EchoCounterFileName));
        CycleHelper.ResetCounter(Path.Combine(config.LocalUploadFilePath, ResponseCounterFileName));

        await creditCardService.SendCurrentCCDataToComerica();

        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneConfig.TimeZoneJobs);
        RecurringJob.AddOrUpdate<VerifyComericaEchoJob>(
            config.VerifyEchoJobIdentifier,
            job => job.Run(),
            config.PollingCronExpression,
            tz);

        logger.LogInformation(
            "UpdateCredtiCardAccount job completed. Scheduled echo verification job '{Identifier}'.",
            config.VerifyEchoJobIdentifier);
    }
}
