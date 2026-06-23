using System;
using System.Collections.Generic;
using System.IO;
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
    private const string ComericaRequestSubdirectory = "request";
    private const int RecordLength = 94;
    private const int MinEchoLineLength = 1;
    private const int StatusFlagIndex = 0;
    private const int MessageStartIndex = 1;
    private const char SuccessFlag = '1';

    private const int MinDetailLength = 78;
    private const int ResponseMerchantNumberStart = 10; // pos 11
    private const int ResponseMerchantNumberLength = 16;
    private const int ResponseOldTokenStart = 26;       // pos 27
    private const int ResponseOldTokenLength = 19;
    private const int ResponseOldExpiryStart = 45;      // pos 46
    private const int ResponseOldExpiryLength = 4;
    private const int ResponseNewTokenStart = 49;       // pos 50
    private const int ResponseNewTokenLength = 19;
    private const int ResponseNewExpiryStart = 68;      // pos 69
    private const int ResponseNewExpiryLength = 4;
    private const int ResponseCodeStart = 72;           // pos 73
    private const int ResponseCodeLength = 6;

    private static readonly HashSet<string> UpdateTokenCodes = new(StringComparer.OrdinalIgnoreCase)
        { "UPDATE", "A", "CC" };
    private static readonly HashSet<string> UpdateExpiryCodes = new(StringComparer.OrdinalIgnoreCase)
        { "EXPIRY", "E", "CE" };
    private static readonly HashSet<string> ContactCardholderCodes = new(StringComparer.OrdinalIgnoreCase)
        { "CONTAC", "C", "XC" };

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
        var creditCardData = await _repository.GetCreditCardsForComericaUpdate();

        // Step 2: Generate the .txt file with the format Comerica expects
        var localFilePath = GenerateComericaFile(
            config.LocalUploadFilePath,
            config.RequestFileName,
            creditCardData,
            config.ChainCode,
            config.MerchantNumber);

        // Step 3: Upload the file to Comerica via SFTP
        var remoteFilePath = Path.Combine(config.RemoteUploadPath, Path.GetFileName(localFilePath));
        await _ftpService.UploadFile(localFilePath, remoteFilePath);

        _logger.LogInformation("SendCurrentCCDataToComerica process completed successfully.");
    }

    public async Task<EchoValidationResult> VerifyComericaRequestDelivery(string remoteEchoPath, string echoFileNamePrefix)
    {
        _logger.LogInformation("Checking for echo file '{Prefix}*' in {RemotePath}.", echoFileNamePrefix, remoteEchoPath);

        var echoFileContent = await _ftpService.DownloadFileContentByPrefix(remoteEchoPath, echoFileNamePrefix);
        if (echoFileContent == null)
        {
            _logger.LogInformation("Echo file '{Prefix}*' not found yet in {RemotePath}.", echoFileNamePrefix, remoteEchoPath);
            return new EchoValidationResult { Status = EchoValidationStatus.NotFound };
        }

        return ParseEchoFileContent(echoFileContent);
    }

    private EchoValidationResult ParseEchoFileContent(string echoFileContent)
    {
        _logger.LogInformation($"Echo file content: {echoFileContent}");

        using var reader = new StringReader(echoFileContent);
        var firstLine = reader.ReadLine();

        _logger.LogInformation($"Echo file: {firstLine}");

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

    public List<AccountUpdaterResponseRecord> ProcessAccountUpdaterResponse(string responseFileContent)
    {
        _logger.LogInformation("Starting Account Updater response file processing.");

        var results = new List<AccountUpdaterResponseRecord>();
        using var reader = new StringReader(responseFileContent);
        var lineNumber = 0;

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;

            if (line.Length == 0)
                continue;

            var recordType = line[0];

            if (recordType is 'H' or 'T')
                continue;

            if (recordType != 'D')
            {
                _logger.LogWarning("Skipping unrecognized record type '{RecordType}' at line {LineNumber}.", recordType, lineNumber);
                continue;
            }

            if (line.Length < MinDetailLength)
            {
                _logger.LogWarning("Skipping detail record at line {LineNumber}: too short ({Length} chars).", lineNumber, line.Length);
                continue;
            }

            var record = ParseResponseDetailRecord(line);

            if (record.Action == ResponseAction.NoChange)
            {
                _logger.LogDebug("Line {LineNumber}: No changes for token {OldToken}, ResponseCode={ResponseCode}.",
                    lineNumber, record.OldToken, record.ResponseCode);
                continue;
            }

            _logger.LogInformation("Line {LineNumber}: Action={Action}, OldToken={OldToken}, ResponseCode={ResponseCode}.",
                lineNumber, record.Action, record.OldToken, record.ResponseCode);

            results.Add(record);
        }

        _logger.LogInformation("Response file processing completed. {ActionableCount} actionable records found.", results.Count);

        // DRY-RUN: database persistence is intentionally disabled for now.
        // We only log what WOULD be updated/saved so it can be reviewed before enabling writes.
        LogPlannedDatabaseChanges(results);

        // TODO: Enable database persistence once the dry-run output has been validated.
        // NOTE: enabling this requires making this method async (Task) so the repository calls can be awaited.
        // foreach (var record in results)
        // {
        //     switch (record.Action)
        //     {
        //         case ResponseAction.UpdateTokenAndExpiry:
        //             await _repository.UpdateCreditCardToken(record.OldToken, record.NewToken, record.NewExpiry);
        //             break;
        //         case ResponseAction.UpdateExpiry:
        //             await _repository.UpdateCreditCardExpiry(record.OldToken, record.NewExpiry);
        //             break;
        //         case ResponseAction.ContactCardholder:
        //             await _repository.MarkCreditCardAsInvalid(record.OldToken);
        //             break;
        //     }
        // }

        return results;
    }

    // DRY-RUN preview: logs exactly what would be written to the database for each actionable record,
    // without persisting anything. Records are matched in DB by BillingCredits.WorldPayToken = OldToken.
    private void LogPlannedDatabaseChanges(IReadOnlyList<AccountUpdaterResponseRecord> records)
    {
        if (records.Count == 0)
        {
            _logger.LogInformation("[DRY-RUN] No database changes to apply (no actionable records).");
            return;
        }

        _logger.LogInformation(
            "[DRY-RUN] Database persistence is DISABLED. Previewing {Count} planned change(s). Nothing will be saved.",
            records.Count);

        var updateTokenAndExpiryCount = 0;
        var updateExpiryCount = 0;
        var contactCardholderCount = 0;

        foreach (var record in records)
        {
            switch (record.Action)
            {
                case ResponseAction.UpdateTokenAndExpiry:
                    updateTokenAndExpiryCount++;
                    _logger.LogInformation(
                        "[DRY-RUN] UPDATE TOKEN + EXPIRY | MerchantNumber={MerchantNumber} | " +
                        "WHERE BillingCredits.WorldPayToken = '{OldToken}' | " +
                        "SET WorldPayToken: '{OldToken}' -> '{NewToken}' | " +
                        "SET Expiry: '{OldExpiry}' -> '{NewExpiry}' (CCExpYear={ExpYear}, CCExpMonth={ExpMonth}) | " +
                        "ResponseCode={ResponseCode}",
                        record.MerchantNumber,
                        record.OldToken,
                        record.OldToken, record.NewToken,
                        record.OldExpiry, record.NewExpiry,
                        FormatExpiryYear(record.NewExpiry), FormatExpiryMonth(record.NewExpiry),
                        record.ResponseCode);
                    break;

                case ResponseAction.UpdateExpiry:
                    updateExpiryCount++;
                    _logger.LogInformation(
                        "[DRY-RUN] UPDATE EXPIRY ONLY | MerchantNumber={MerchantNumber} | " +
                        "WHERE BillingCredits.WorldPayToken = '{OldToken}' (token unchanged) | " +
                        "SET Expiry: '{OldExpiry}' -> '{NewExpiry}' (CCExpYear={ExpYear}, CCExpMonth={ExpMonth}) | " +
                        "ResponseCode={ResponseCode}",
                        record.MerchantNumber,
                        record.OldToken,
                        record.OldExpiry, record.NewExpiry,
                        FormatExpiryYear(record.NewExpiry), FormatExpiryMonth(record.NewExpiry),
                        record.ResponseCode);
                    break;

                case ResponseAction.ContactCardholder:
                    contactCardholderCount++;
                    _logger.LogWarning(
                        "[DRY-RUN] CONTACT CARDHOLDER (no automatic update) | MerchantNumber={MerchantNumber} | " +
                        "BillingCredits.WorldPayToken = '{OldToken}' should be flagged/reviewed. " +
                        "No new token/expiry provided by Comerica. | ResponseCode={ResponseCode}",
                        record.MerchantNumber, record.OldToken, record.ResponseCode);
                    break;
            }
        }

        _logger.LogInformation(
            "[DRY-RUN] Planned changes summary -> UpdateTokenAndExpiry: {UpdateTokenAndExpiry}, " +
            "UpdateExpiry: {UpdateExpiry}, ContactCardholder: {ContactCardholder}.",
            updateTokenAndExpiryCount, updateExpiryCount, contactCardholderCount);
    }

    // Comerica expiry is expected in YYMM format (same as the request file).
    private static string FormatExpiryYear(string yyMm)
        => yyMm is { Length: 4 } ? "20" + yyMm.Substring(0, 2) : "(invalid)";

    private static string FormatExpiryMonth(string yyMm)
        => yyMm is { Length: 4 } ? yyMm.Substring(2, 2) : "(invalid)";

    private static AccountUpdaterResponseRecord ParseResponseDetailRecord(string line)
    {
        var merchantNumber = line.Substring(ResponseMerchantNumberStart, ResponseMerchantNumberLength).Trim();
        var oldToken = line.Substring(ResponseOldTokenStart, ResponseOldTokenLength).Trim();
        var oldExpiry = line.Substring(ResponseOldExpiryStart, ResponseOldExpiryLength).Trim();
        var newToken = line.Substring(ResponseNewTokenStart, ResponseNewTokenLength).Trim();
        var newExpiry = line.Substring(ResponseNewExpiryStart, ResponseNewExpiryLength).Trim();
        var responseCode = line.Substring(ResponseCodeStart, ResponseCodeLength).Trim();

        var action = DetermineResponseAction(responseCode, newToken, newExpiry);

        return new AccountUpdaterResponseRecord
        {
            MerchantNumber = merchantNumber,
            OldToken = oldToken,
            OldExpiry = oldExpiry,
            NewToken = newToken,
            NewExpiry = newExpiry,
            ResponseCode = responseCode,
            Action = action
        };
    }

    private static ResponseAction DetermineResponseAction(string responseCode, string newToken, string newExpiry)
    {
        if (ContactCardholderCodes.Contains(responseCode))
            return ResponseAction.ContactCardholder;

        if (UpdateTokenCodes.Contains(responseCode))
            return ResponseAction.UpdateTokenAndExpiry;

        if (UpdateExpiryCodes.Contains(responseCode))
            return ResponseAction.UpdateExpiry;

        if (!string.IsNullOrEmpty(newToken) && !string.IsNullOrEmpty(newExpiry))
            return ResponseAction.UpdateTokenAndExpiry;

        if (!string.IsNullOrEmpty(newExpiry))
            return ResponseAction.UpdateExpiry;

        return ResponseAction.NoChange;
    }

    private string GenerateComericaFile(
        string localDirectory,
        string fileName,
        IEnumerable<CreditCardData> records,
        string chainCode,
        string merchantNumber)
    {
        var outputDirectory = Path.Combine(localDirectory, ComericaRequestSubdirectory);
        Directory.CreateDirectory(outputDirectory);
        var filePath = Path.Combine(outputDirectory, fileName);

        _logger.LogInformation("Generating Comerica file: {FileName}", fileName);

        var sb = new StringBuilder();
        sb.AppendLine(BuildHeaderRecord());

        var detailCount = 0;
        foreach (var record in records)
        {
            sb.AppendLine(BuildDetailRecord(record, chainCode, merchantNumber));
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

    private static string BuildDetailRecord(CreditCardData data, string chainCode, string merchantNumber)
    {
        var sb = new StringBuilder(RecordLength);

        // Pos 1: Record type
        sb.Append('D');
        // Pos 2-10: Chain Code (9 chars, zero-padded left)
        sb.Append((chainCode ?? "").PadLeft(9, '0'));
        // Pos 11-26: Merchant Number (16 chars)
        sb.Append((merchantNumber ?? "").PadRight(16));
        // Pos 27-45: Token (19 chars, space-padded right)
        sb.Append((data.Token ?? "").PadRight(19));
        // Pos 46-49: Expiry Date YYMM (4 chars)
        sb.Append((data.ExpiryDate ?? "").PadRight(4));
        // Pos 50: Tokenized Flag
        sb.Append('T');
        // Pos 51-94: Filler (44 spaces)
        sb.Append(' ', 44);

        return sb.ToString();
    }

    private static string BuildTrailerRecord(int detailCount)
    {
        return $"T{detailCount.ToString().PadLeft(9, '0')}".PadRight(RecordLength);
    }
}
