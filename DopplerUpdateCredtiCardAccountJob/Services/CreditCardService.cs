using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doppler.UpdateCredtiCardAccount.Job.Database;
using Doppler.UpdateCredtiCardAccount.Job.Entities;
using Doppler.UpdateCredtiCardAccount.Job.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.UpdateCredtiCardAccount.Job.Services;

public class CreditCardService : ICreditCardService
{
    private const string ComericaRequestSubdirectory = "comerica/request";
    private const int RecordLength = 94;
    private const int MinEchoLineLength = 8;
    private const int StatusFlagIndex = 7;
    private const int MessageStartIndex = 8;
    private const char SuccessFlag = '1';

    private readonly ILogger<CreditCardService> _logger;
    private readonly IDopplerRepository _repository;
    private readonly IFtpService _ftpService;
    private readonly IOptionsMonitor<UpdateCredtiCardAccountJobSettings> _settings;

    public CreditCardService(
        ILogger<CreditCardService> logger,
        IDopplerRepository repository,
        IFtpService ftpService,
        IOptionsMonitor<UpdateCredtiCardAccountJobSettings> settings)
    {
        _logger = logger;
        _repository = repository;
        _ftpService = ftpService;
        _settings = settings;
    }

    public async Task SendCurrentCCDataToComerica()
    {
        var config = _settings.CurrentValue;

        _logger.LogInformation("Starting SendCurrentCCDataToComerica process.");

        // Step 1: Get current credit card data from the database
        // TODO: Define the repository method to retrieve CC data
        // var creditCardData = await _repository.GetCurrentCreditCardData();

        // Step 2: Generate the .txt file with the format Comerica expects
        var localFilePath = GenerateComericaFile(config.LocalUploadFilePath);

        // Step 3: Upload the file to Comerica via SFTP
        var remoteFilePath = Path.Combine(config.RemoteUploadPath, Path.GetFileName(localFilePath));
        await _ftpService.UploadFile(localFilePath, remoteFilePath);

        _logger.LogInformation("SendCurrentCCDataToComerica process completed successfully.");
    }

    public async Task<EchoValidationResult> ValidateEchoFile(string remoteEchoFilePath)
    {
        _logger.LogInformation("Checking for echo file at {RemotePath}.", remoteEchoFilePath);

        string echoFileContent;
        try
        {
            echoFileContent = await _ftpService.DownloadFileContent(remoteEchoFilePath);
        }
        catch (Renci.SshNet.Common.SftpPathNotFoundException)
        {
            _logger.LogInformation("Echo file not found yet at {RemotePath}.", remoteEchoFilePath);
            return new EchoValidationResult { Status = EchoValidationStatus.NotFound };
        }

        return ParseEchoFileContent(echoFileContent);
    }

    private EchoValidationResult ParseEchoFileContent(string echoFileContent)
    {
        using var reader = new StringReader(echoFileContent);
        var firstLine = reader.ReadLine();

        if (firstLine == null || firstLine.Length < MinEchoLineLength)
        {
            _logger.LogError("Invalid echo file format. Line is null or too short (length: {Length}).",
                firstLine?.Length ?? 0);
            return new EchoValidationResult
            {
                Status = EchoValidationStatus.InvalidFormat,
                ErrorMessage = "Formato de archivo inválido"
            };
        }

        var statusFlag = firstLine[StatusFlagIndex];
        var message = firstLine.Length > MessageStartIndex
            ? firstLine[MessageStartIndex..].Trim()
            : string.Empty;

        if (statusFlag == SuccessFlag)
        {
            _logger.LogInformation("Echo file validation succeeded.");
            return new EchoValidationResult { Status = EchoValidationStatus.Success };
        }

        _logger.LogWarning("Echo file validation failed. Status: {StatusFlag}, Message: {Message}.",
            statusFlag, message);
        return new EchoValidationResult
        {
            Status = EchoValidationStatus.Failed,
            ErrorMessage = message
        };
    }

    private string GenerateComericaFile(string localDirectory)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"DOPP_AU_REQ_{timestamp}.txt";
        var outputDirectory = Path.Combine(localDirectory, ComericaRequestSubdirectory);
        Directory.CreateDirectory(outputDirectory);
        var filePath = Path.Combine(outputDirectory, fileName);

        _logger.LogInformation("Generating Comerica file: {FileName}", fileName);

        // TODO: Replace with actual data from repository
        var records = Enumerable.Empty<CreditCardData>();

        var sb = new StringBuilder();
        sb.AppendLine(BuildHeaderRecord());

        var detailCount = 0;
        foreach (var record in records)
        {
            sb.AppendLine(BuildDetailRecord(record));
            detailCount++;
        }

        sb.AppendLine(BuildTrailerRecord(detailCount));

        File.WriteAllText(filePath, sb.ToString());

        _logger.LogInformation("Comerica file generated at: {FilePath}", filePath);

        return filePath;
    }

    private static string BuildHeaderRecord()
    {
        var date = DateTime.UtcNow.ToString("yyMMdd");
        return $"H{date}".PadRight(RecordLength);
    }

    private static string BuildDetailRecord(CreditCardData data)
    {
        var sb = new StringBuilder(RecordLength);

        // Pos 1: Record type
        sb.Append('D');
        // Pos 2-10: Chain Code (9 chars, zero-padded left)
        sb.Append(data.ChainCode.ToString().PadLeft(9, '0'));
        // Pos 11-26: Merchant Number (16 chars)
        sb.Append((data.MerchantNumber ?? "").PadRight(16));
        // Pos 27-45: Token (19 chars, space-padded right)
        sb.Append((data.Token ?? "").PadRight(19));
        // Pos 46-49: Expiry Date YYMM (4 chars)
        sb.Append((data.ExpiryDate ?? "").PadRight(4));
        // Pos 50-59: Filler (10 spaces)
        sb.Append(' ', 10);
        // Pos 60: Tokenized Flag
        sb.Append('T');
        // Pos 61-94: Filler (34 spaces)
        sb.Append(' ', 34);

        return sb.ToString();
    }

    private static string BuildTrailerRecord(int detailCount)
    {
        return $"T{detailCount.ToString().PadLeft(9, '0')}".PadRight(RecordLength);
    }
}
