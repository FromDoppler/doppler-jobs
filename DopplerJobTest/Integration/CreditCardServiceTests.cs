using Doppler.UpdateCredtiCardAccount.Job.Database;
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

    public CreditCardServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual test - run only to verify request file generation and SFTP upload")]
    public async Task SendCurrentCCDataToComerica_WithMockData_ShouldGenerateAndUploadFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "comerica-test");

        var settings = new UpdateCredtiCardAccountJobSettings
        {
            Host = "reporting.fromdoppler.com",
            Port = 9427,
            Username = "comericaFTP",
            Password = "ziYujTQaMm4hpSJS",
            LocalUploadFilePath = tempDir,
            RemoteUploadPath = "/upload"
        };

        var mockRepository = new Mock<IDopplerRepository>();
        var loggerCreditCard = new Mock<ILogger<CreditCardService>>();
        var loggerFtp = new Mock<ILogger<FtpService>>();
        var optionsMonitor = new Mock<IOptionsMonitor<UpdateCredtiCardAccountJobSettings>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(settings);

        var realFtpService = new FtpService(loggerFtp.Object, optionsMonitor.Object);

        var service = new CreditCardService(
            loggerCreditCard.Object,
            mockRepository.Object,
            realFtpService,
            optionsMonitor.Object);

        // Act
        await service.SendCurrentCCDataToComerica();

        // Assert
        var outputDir = Path.Combine(tempDir, "comerica", "request");
        Assert.True(Directory.Exists(outputDir), "Output directory should be created");

        var files = Directory.GetFiles(outputDir, "DOPP_AU_REQ_*.txt");
        Assert.Single(files);

        var content = await File.ReadAllTextAsync(files[0]);
        _output.WriteLine("Generated file content:");
        _output.WriteLine(content);

        Assert.Contains("H", content);
        Assert.Contains("T", content);

        Directory.Delete(tempDir, true);
    }
}
