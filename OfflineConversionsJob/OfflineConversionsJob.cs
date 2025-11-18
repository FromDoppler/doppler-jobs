using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hangfire;
using OfflineConversionsJob.Services;

namespace OfflineConversionsJob;

public class OfflineConversionsJob(
    ILogger<OfflineConversionsJob> logger,
    IGoogleConversionService googleConversionService)
{
    [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
    public void Run() => RunAsync().GetAwaiter().GetResult();

    private async Task RunAsync()
    {
        logger.LogInformation("Starting offline conversion from ZOHO to Google Ad.");
        await googleConversionService.UploadConversionsToGoogle();
    }
}