using Doppler.UpdateCredtiCardAccount.Job.Database;
using Doppler.UpdateCredtiCardAccount.Job.Entities;
using Doppler.UpdateCredtiCardAccount.Job.Services;
using Doppler.UpdateCredtiCardAccount.Job.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Doppler.Jobs.Test.Integration;

public class CreditCardServiceTests
{
    private readonly ITestOutputHelper _output;

    private static readonly UpdateCredtiCardAccountJobSettings DefaultSettings = new()
    {
        Host = "<HOST>",
        Port = 1,
        Username = "<USER>",
        Password = "<PASSWORD>",
        LocalUploadFilePath = Path.Combine(Path.GetTempPath(), "comerica-test"),
        RemoteUploadPath = "/upload"
    };

    public CreditCardServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private CreditCardService CreateService(UpdateCredtiCardAccountJobSettings settings = null)
    {
        settings ??= DefaultSettings;

        var mockRepository = new Mock<IDopplerRepository>();
        var loggerCreditCard = new Mock<ILogger<CreditCardService>>();
        var loggerFtp = new Mock<ILogger<FtpService>>();
        var optionsMonitor = new Mock<IOptionsMonitor<UpdateCredtiCardAccountJobSettings>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(settings);

        var realFtpService = new FtpService(loggerFtp.Object, optionsMonitor.Object);

        return new CreditCardService(
            loggerCreditCard.Object,
            mockRepository.Object,
            realFtpService,
            optionsMonitor.Object);
    }

    [Fact(Skip = "Manual test - run only to verify request file generation and SFTP upload")]
    public async Task SendCurrentCCDataToComerica_WithMockData_ShouldGenerateAndUploadFile()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SendCurrentCCDataToComerica();

        // Assert
        var outputDir = Path.Combine(DefaultSettings.LocalUploadFilePath, "comerica", "request");
        Assert.True(Directory.Exists(outputDir), "Output directory should be created");

        var files = Directory.GetFiles(outputDir, "DOPP_R0BCRPPU.txt");
        Assert.Single(files);

        var content = await File.ReadAllTextAsync(files[0]);
        _output.WriteLine("Generated file content:");
        _output.WriteLine(content);

        Assert.Contains("H", content);
        Assert.Contains("T", content);

        Directory.Delete(DefaultSettings.LocalUploadFilePath, true);
    }

    [Fact(Skip = "Manual test - run only to verify echo file validation against real SFTP")]
    public async Task VerifyComericaRequestDelivery_WithRealSftp_ShouldReturnExpectedStatus()
    {
        // Arrange
        var service = CreateService();

        // Make sure this file exists in the FTP Server
        var remoteEchoFilePath = "/download/DOPP_AU_ECHO_SAMPLE.txt";

        // Act
        var result = await service.VerifyComericaRequestDelivery(remoteEchoFilePath);

        // Assert
        _output.WriteLine($"Status: {result.Status}");
        _output.WriteLine($"ErrorMessage: {result.ErrorMessage ?? "(none)"}");

        Assert.True(
            result.Status is EchoValidationStatus.Success,
            $"Unexpected status: {result.Status}");
    }

    [Fact(Skip = "Manual test - run only to verify echo file validation against real SFTP")]
    public async Task VerifyComericaRequestDelivery_WithRealSftp_ShouldReturnFailedStatus()
    {
        // Arrange
        var service = CreateService();

        // Make sure this file exists in the FTP Server
        var remoteEchoFilePath = "/download/DOPP_AU_ECHO_SAMPLE_failed.txt";

        // Act
        var result = await service.VerifyComericaRequestDelivery(remoteEchoFilePath);

        // Assert
        _output.WriteLine($"Status: {result.Status}");
        _output.WriteLine($"ErrorMessage: {result.ErrorMessage ?? "(none)"}");

        Assert.True(
            result.Status is EchoValidationStatus.Failed,
            $"Unexpected status: {result.Status}");
    }

    [Fact(Skip = "Manual test - run only to verify echo file validation against real SFTP")]
    public async Task VerifyComericaRequestDelivery_WithRealSftp_ShouldReturnNotFoundStatus()
    {
        // Arrange
        var service = CreateService();

        // Make sure this file does not exists in the FTP Server
        var remoteEchoFilePath = "/download-empty/DOPP_AU_ECHO_SAMPLE.txt";

        // Act
        var result = await service.VerifyComericaRequestDelivery(remoteEchoFilePath);

        // Assert
        _output.WriteLine($"Status: {result.Status}");
        _output.WriteLine($"ErrorMessage: {result.ErrorMessage ?? "(none)"}");

        Assert.True(
            result.Status is EchoValidationStatus.NotFound,
            $"Unexpected status: {result.Status}");
    }

    [Fact(Skip = "Manual test - upload response file to SFTP before running")]
    public async Task ProcessAccountUpdaterResponse_WithRealSftp_ShouldParseResponseFile()
    {
        // Arrange
        var service = CreateService();

        // Make sure this file exists in the FTP Server
        var remoteResponseFilePath = "/download/DOPP_AU_RESP_SAMPLE.txt";

        // Act
        var optionsMonitor = new Mock<IOptionsMonitor<UpdateCredtiCardAccountJobSettings>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(DefaultSettings);
        var responseFileContent = await new FtpService(
            new Mock<ILogger<FtpService>>().Object,
            optionsMonitor.Object).DownloadFileContent(remoteResponseFilePath);

        var results = service.ProcessAccountUpdaterResponse(responseFileContent);

        // Assert
        _output.WriteLine($"Total actionable records: {results.Count}");
        foreach (var record in results)
        {
            _output.WriteLine($"Action={record.Action}, OldToken={record.OldToken}, NewToken={record.NewToken}, " +
                              $"OldExpiry={record.OldExpiry}, NewExpiry={record.NewExpiry}, ResponseCode={record.ResponseCode}");
        }

        Assert.NotNull(results);

        var updateTokenRecords = results.Where(r => r.Action == ResponseAction.UpdateTokenAndExpiry).ToList();
        var updateExpiryRecords = results.Where(r => r.Action == ResponseAction.UpdateExpiry).ToList();
        var contactRecords = results.Where(r => r.Action == ResponseAction.ContactCardholder).ToList();

        _output.WriteLine($"\nSummary:");
        _output.WriteLine($"  UpdateTokenAndExpiry: {updateTokenRecords.Count}");
        _output.WriteLine($"  UpdateExpiry: {updateExpiryRecords.Count}");
        _output.WriteLine($"  ContactCardholder: {contactRecords.Count}");
    }

}
