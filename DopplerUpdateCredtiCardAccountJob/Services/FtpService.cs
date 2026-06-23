using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Doppler.UpdateCredtiCardAccount.Job.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace Doppler.UpdateCredtiCardAccount.Job.Services;

public class FtpService : IFtpService
{
    private readonly ILogger<FtpService> _logger;
    private readonly UpdateCredtiCardAccountJobSettings _settings;

    public FtpService(ILogger<FtpService> logger, IOptionsMonitor<UpdateCredtiCardAccountJobSettings> settings)
    {
        _logger = logger;
        _settings = settings.CurrentValue;
    }

    private SftpClient CreateClient()
    {
        return new SftpClient(_settings.Host, _settings.Port, _settings.Username, _settings.Password);
    }

    public Task UploadFile(string localFilePath, string remoteFilePath)
    {
        try
        {
            using var client = CreateClient();
            client.Connect();
            _logger.LogInformation("Connected to SFTP server {Host}. Uploading {LocalFile} to {RemoteFile}.",
                _settings.Host, localFilePath, remoteFilePath);

            using var fileStream = File.OpenRead(localFilePath);
            client.UploadFile(fileStream, remoteFilePath, true);

            _logger.LogInformation("File uploaded successfully to {RemoteFile}.", remoteFilePath);

            client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SFTP upload/connect failed for {Host}:{Port}. Local file: {LocalFilePath}. Remote path: {RemoteFilePath}.",
                _settings.Host, _settings.Port, localFilePath, remoteFilePath);
        }

        return Task.CompletedTask;
    }

    public Task<string> DownloadFileContent(string remoteFilePath)
    {
        using var client = CreateClient();
        client.Connect();
        _logger.LogInformation("Connected to SFTP server {Host}. Downloading {RemoteFile}.",
            _settings.Host, remoteFilePath);

        using var memoryStream = new MemoryStream();
        client.DownloadFile(remoteFilePath, memoryStream);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var content = reader.ReadToEnd();

        _logger.LogInformation("File {RemoteFile} downloaded successfully. Content length: {Length} chars.",
            remoteFilePath, content.Length);

        client.Disconnect();
        return Task.FromResult(content);
    }

    public Task<bool> CheckConnection()
    {
        try
        {
            using var client = CreateClient();
            client.Connect();
            var isConnected = client.IsConnected;
            _logger.LogInformation("SFTP health check to {Host}:{Port} - Connected: {IsConnected}.",
                _settings.Host, _settings.Port, isConnected);
            client.Disconnect();
            return Task.FromResult(isConnected);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "SFTP health check failed for {Host}:{Port}.", _settings.Host, _settings.Port);
            return Task.FromResult(false);
        }
    }

    public Task<IEnumerable<string>> ListFiles(string remotePath)
    {
        using var client = CreateClient();
        client.Connect();
        _logger.LogInformation("Connected to SFTP server {Host}. Listing files in {RemotePath}.",
            _settings.Host, remotePath);

        var files = client.ListDirectory(remotePath)
            .Where(f => !f.IsDirectory)
            .Select(f => f.Name);

        var result = files.ToList();

        _logger.LogInformation("Found {Count} files in {RemotePath}.", result.Count, remotePath);

        client.Disconnect();
        return Task.FromResult<IEnumerable<string>>(result);
    }

    public Task<string> DownloadFileContentByPrefix(string remotePath, string fileNamePrefix)
    {
        using var client = CreateClient();
        client.Connect();
        _logger.LogInformation("Connected to SFTP server {Host}. Searching for files starting with '{Prefix}' in {RemotePath}.",
            _settings.Host, fileNamePrefix, remotePath);

        var remoteFile = client.ListDirectory(remotePath)
            .Where(f => !f.IsDirectory && f.Name.StartsWith(fileNamePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.Name)
            .Select(f => f.FullName)
            .FirstOrDefault();

        if (remoteFile == null)
        {
            _logger.LogInformation("No file starting with '{Prefix}' found in {RemotePath}.", fileNamePrefix, remotePath);
            client.Disconnect();
            return Task.FromResult<string>(null);
        }

        _logger.LogInformation("Resolved file '{RemoteFile}' for prefix '{Prefix}'. Downloading.", remoteFile, fileNamePrefix);

        using var memoryStream = new MemoryStream();
        client.DownloadFile(remoteFile, memoryStream);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var content = reader.ReadToEnd();

        _logger.LogInformation("File {RemoteFile} downloaded successfully. Content length: {Length} chars.",
            remoteFile, content.Length);

        client.Disconnect();
        return Task.FromResult(content);
    }
}
