namespace Doppler.Ftp.Job.Settings;

public class FtpJobSettings
{
    public string Host { get; set; }
    public int Port { get; set; } = 22;
    public string Username { get; set; }
    public string Password { get; set; }
    public string RemoteUploadPath { get; set; }
    public string RemoteDownloadPath { get; set; }
    public string LocalUploadFilePath { get; set; }
    public string LocalDownloadDirectory { get; set; }
}
