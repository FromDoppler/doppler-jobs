using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Ads.GoogleAds.Config;
using Google.Ads.GoogleAds.Lib;
using Google.Ads.GoogleAds.V22.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfflineConversionsJob.ApiClient;
using OfflineConversionsJob.Entities;
using OfflineConversionsJob.Settings;

namespace OfflineConversionsJob.Services;

public class GoogleConversionService(
    ILogger<GoogleConversionService> logger, 
    IOptionsMonitor<OfflineConversionsJobSettings> offlineConversionsJobSettings,
    IZohoApiClient zohoApiClient
    ) : IGoogleConversionService
{
    private readonly string _conversionActionResourceName = $"customers/{offlineConversionsJobSettings.CurrentValue.GoogleClientCustomerId}/conversionActions/{offlineConversionsJobSettings.CurrentValue.GoogleConversionActionId}";
    
    public async Task UploadConversionsToGoogle()
    {
        logger.LogInformation("Uploading conversions to Google.");
        var configClient = new GoogleAdsConfig
        {
            DeveloperToken = offlineConversionsJobSettings.CurrentValue.GoogleDeveloperToken,
            LoginCustomerId = offlineConversionsJobSettings.CurrentValue.GoogleLoginCustomerId,
            OAuth2ClientId = offlineConversionsJobSettings.CurrentValue.GoogleOAuthClientId,
            OAuth2ClientSecret = offlineConversionsJobSettings.CurrentValue.GoogleOAuthClientSecret,
            OAuth2RefreshToken = offlineConversionsJobSettings.CurrentValue.GoogleAuthRefreshToken
        };
        
        var client = new GoogleAdsClient(configClient);
        var googleService = client.GetService(Google.Ads.GoogleAds.Services.V22.ConversionUploadService);
        
        logger.LogInformation("Getting new leads from ZOHO database.");
        var leadsFromZoho = await GetQualifiedLeadsFromZoho();
        if (!leadsFromZoho.Any())
        {
            logger.LogInformation("No new leads found to process.");
            return;
        }
        
        var conversions = ZohoQualifiedLeadsToGoogleConversions(leadsFromZoho);
        
        var validationRequest = CreateUploadRequest(conversions, true);
        var validationResponse = await googleService.UploadClickConversionsAsync(validationRequest);

        var errorLeads = GetErrorsFromGoogleResponse(conversions, validationResponse);
        var leadsToUpload = conversions.Except(errorLeads).ToList();
        
        if (!leadsToUpload.Any())
        {
            logger.LogWarning("All conversions had errors during validation. No conversions to upload.");
            return;
        }
        
        logger.LogInformation("Validated {ValidConversionCount} conversions to upload to Google after removing {ErrorCount} errors.", leadsToUpload.Count, errorLeads.Count);
        
        var actualRequest = CreateUploadRequest(leadsToUpload, false);
        var actualResponse = await googleService.UploadClickConversionsAsync(actualRequest);
        
        var failedConversions = GetErrorsFromGoogleResponse(leadsToUpload, actualResponse);
        if (failedConversions.Any())
            logger.LogError("Some conversions failed to upload with the actual request. Please check the logs for details.");
        
        logger.LogInformation("Uploaded {ConversionCount} conversions to Google. JobId = {jobId}", (actualResponse.Results.Count - failedConversions.Count), actualResponse.JobId);
        
        var uploadedConversions = leadsToUpload.Except(failedConversions).ToList();
        
        var leadsToMarkAsProcessed = uploadedConversions.Select(uc => new UpdateZohoQualifiedLead
        {
            LeadId = uc.OrderId,
            GoogleAdProcessed = true
        }).ToList();
        
        await UpdateLeadsAsProcessedInZoho(leadsToMarkAsProcessed);
    }
    
    private async Task<List<ZohoQualifiedLead>> GetQualifiedLeadsFromZoho()
    {
        logger.LogInformation("Generating  auth token for ZOHO API.");
        var accessToken = await zohoApiClient.GetAccessTokenAsync();
        
        logger.LogInformation("Fetching qualified leads from ZOHO API.");
        var leads = await zohoApiClient.GetZohoQualifiedLeadsAsync(accessToken);

        return leads;
    }
    
    private async Task UpdateLeadsAsProcessedInZoho(List<UpdateZohoQualifiedLead> leads)
    {
        // Zoho API only accepts chunks of 100 records for updates. Just in case we can split it to a little bit less.
        var leadsChunks = leads
            .Select((lead, index) => new { lead, index })
            .GroupBy(x => x.index / 90)
            .Select(g => g.Select(x => x.lead).ToList())
            .ToList();
        
        var accessToken = await zohoApiClient.GetAccessTokenAsync();
        var updatedCount = 0;
        
        foreach (var chunk in leadsChunks)
        {
            var successCount = await zohoApiClient.UpdateZohoQualifiedLeadsAsync(accessToken, chunk);
            updatedCount += successCount;
        }
        
        logger.LogInformation("Updated {UpdatedCount} leads as processed in ZOHO.", updatedCount);
    }

    private List<ClickConversion> GetErrorsFromGoogleResponse(List<ClickConversion> leads, UploadClickConversionsResponse response)
    {
        var errorLeads = new List<ClickConversion>();
        
        if (response.PartialFailure?.Errors?.Any() != true)
        {
            logger.LogInformation("No errors found in Google response.");
            return errorLeads;
        }
        
        foreach (var error in response.PartialFailure.Errors)
        {
            var errorIndex = error.Location.FieldPathElements.First(fpe => fpe.FieldName == "conversions").Index;
            errorLeads.Add(leads[errorIndex]);
            logger.LogError("Error for lead {id}: {ErrorMessage}", leads[errorIndex].OrderId, error.Message);
        }
        
        return errorLeads;
    }
    
    private UploadClickConversionsRequest CreateUploadRequest(List<ClickConversion> conversions, bool validateOnly)
    {
        return new UploadClickConversionsRequest
        {
            CustomerId = offlineConversionsJobSettings.CurrentValue.GoogleClientCustomerId,
            Conversions = { conversions },
            PartialFailure = true,
            ValidateOnly = validateOnly,
        };
    }
    
    private List<ClickConversion> ZohoQualifiedLeadsToGoogleConversions(List<ZohoQualifiedLead> leads)
    {
        var conversions = new List<ClickConversion>();
        foreach (var lead in leads)
        {
            logger.LogInformation("Processing lead with ID: {LeadId}", lead.LeadId);
            var conversion = new ClickConversion
            {
                Gclid = lead.GoogleClickId,
                ConversionAction = _conversionActionResourceName,
                ConversionDateTime = lead.ConversionDateTime.ToString("yyyy-MM-dd HH:mm:sszzz"),
                ConversionValue = lead.ConversionValue,
                CurrencyCode = lead.CurrencyCode ?? "USD",
                OrderId = lead.LeadId
            };
            conversions.Add(conversion);
        }

        return conversions;
    }
}