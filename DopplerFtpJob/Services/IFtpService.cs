using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.Ftp.Job.Services;

public interface IFtpService
{
    Task UploadFile(string localFilePath, string remoteFilePath);
    Task<string> DownloadFileContent(string remoteFilePath);
    Task<bool> CheckConnection();
    Task<IEnumerable<string>> ListFiles(string remotePath);
}
