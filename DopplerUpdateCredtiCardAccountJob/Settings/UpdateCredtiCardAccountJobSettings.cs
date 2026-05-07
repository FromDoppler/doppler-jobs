namespace Doppler.UpdateCredtiCardAccount.Job.Settings;

public class UpdateCredtiCardAccountJobSettings
{
    public string Host { get; set; }
    public int Port { get; set; } = 22;
    public string Username { get; set; }
    public string Password { get; set; }
    public long ChainCode { get; set; }
    public string MerchantNumber { get; set; }
    public string RemoteUploadPath { get; set; }
    public string LocalUploadFilePath { get; set; }
    public string RemoteEchoPath { get; set; }
    public string RemoteResponsePath { get; set; }
    public string RequestFileName { get; set; }
    public string EchoFileName { get; set; }
    public string ResponseFileName { get; set; }
    public string PollingCronExpression { get; set; }
    public string VerifyEchoJobIdentifier { get; set; }
    public string ProcessResponseJobIdentifier { get; set; }
    public int MaxPollingRetries { get; set; } = 10;
}
