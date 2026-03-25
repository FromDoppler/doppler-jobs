using System.IO;
using System.Threading.Tasks;
using Doppler.Ftp.Job.Services;
using Doppler.Ftp.Job.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Doppler.Jobs.Test.Integration;

public class SftpConnectionTests
{
    private readonly ITestOutputHelper _output;

    public SftpConnectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private FtpService CreateService()
    {
        var settings = new FtpJobSettings
        {
            Host = "reporting.fromdoppler.com",
            Port = 9427,
            Username = "comericaFTP",
            Password = "ziYujTQaMm4hpSJS"
        };

        var logger = new Mock<ILogger<FtpService>>();
        var optionsMonitor = new Mock<IOptionsMonitor<FtpJobSettings>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(settings);

        return new FtpService(logger.Object, optionsMonitor.Object);
    }

    [Fact(Skip = "Manual test - run only to verify SFTP connectivity")]
    public async Task CheckConnection_WithRealServer_ShouldConnectSuccessfully()
    {
        var service = CreateService();

        var result = await service.CheckConnection();

        Assert.True(result, "SFTP connection should succeed with the provided credentials.");
    }
}
