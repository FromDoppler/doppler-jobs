namespace Doppler.UpdateCredtiCardAccount.Job.Settings;

public class UpdateCredtiCardAccountJobSettings
{
    public string Host { get; set; }
    public int Port { get; set; } = 22;
    public string Username { get; set; }
    public string Password { get; set; }
    public string RemoteUploadPath { get; set; }
    public string RemoteDownloadPath { get; set; }
    public string LocalUploadFilePath { get; set; }
    public string LocalDownloadDirectory { get; set; }
    public string RemoteEchoPath { get; set; }
    public string RemoteResponsePath { get; set; }
    public string EchoFilePrefix { get; set; }
    public string ResponseFilePrefix { get; set; }
    public string PollingCronExpression { get; set; }
    public string VerifyEchoJobIdentifier { get; set; }
    public string ProcessResponseJobIdentifier { get; set; }
    public int MaxPollingRetries { get; set; } = 10;
}
