using Doppler.UpdateCredtiCardAccount.Job.Database;
using Doppler.UpdateCredtiCardAccount.Job.Entities;
using Doppler.UpdateCredtiCardAccount.Job.Services;
using Doppler.UpdateCredtiCardAccount.Job.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Doppler.Jobs.Test.Integration;

public class CreditCardServiceTests
{
    private readonly ITestOutputHelper _output;

    private static readonly UpdateCredtiCardAccountJobSettings DefaultSettings = new()
    {
        Host = "reporting.fromdoppler.com",
        Port = 9427,
        Username = "comericaFTP",
        Password = "ziYujTQaMm4hpSJS",
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

        var files = Directory.GetFiles(outputDir, "DOPP_AU_REQ_*.txt");
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
}
