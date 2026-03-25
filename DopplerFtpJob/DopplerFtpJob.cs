using System.Threading.Tasks;
using Doppler.Ftp.Job.Services;
using Doppler.Ftp.Job.Settings;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.Ftp.Job;

public class DopplerFtpJob(
    ILogger<DopplerFtpJob> logger,
    IFtpService ftpService,
    IOptionsMonitor<FtpJobSettings> settings)
{
    [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
    public void Run() => RunAsync().GetAwaiter().GetResult();

    private async Task RunAsync()
    {
        var config = settings.CurrentValue;

        logger.LogInformation("Starting FTP job.");

        logger.LogInformation("Uploading file {LocalFile} to {RemotePath}.",
            config.LocalUploadFilePath, config.RemoteUploadPath);
        await ftpService.UploadFile(config.LocalUploadFilePath, config.RemoteUploadPath);

        logger.LogInformation("Downloading file from {RemotePath}.", config.RemoteDownloadPath);
        var content = await ftpService.DownloadFileContent(config.RemoteDownloadPath);

        logger.LogInformation("Downloaded file content:\n{Content}", content);

        logger.LogInformation("FTP job completed successfully.");
    }
}
