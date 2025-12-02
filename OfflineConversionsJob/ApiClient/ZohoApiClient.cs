using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using OfflineConversionsJob.Entities;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfflineConversionsJob.Settings;

namespace OfflineConversionsJob.ApiClient;

public class ZohoApiClient(HttpClient httpClient, ILogger<ZohoApiClient> logger, IOptionsMonitor<OfflineConversionsJobSettings> offlineConversionsJobSettings) : IZohoApiClient
{
    public async Task<List<ZohoQualifiedLead>> GetZohoQualifiedLeadsAsync(string accessToken)
    {
        List<ZohoQualifiedLead> leads = [];
        var baseUrl = "https://www.zohoapis.com/crm/v8/Leads/search";
        var moreRecords = false;
        
        var query = new ZohoQualifiedLeadQuery
        {
            Fields = new List<string> { "GCLID", "Created_Time", "Score", "Currency", "Id", "GoogleAdProcessed" },
            PageSize = 200, // Max allowed by Zoho.
            Page = 1,
            FromCreatedTime = DateTimeOffset.Now.AddDays(-90).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            MinScore = 1,
            GoogleAdProcessed = false,
            SortBy = "Created_Time",
            SortOrder = "desc"
        };

        do
        {
            var url = $"{baseUrl}?{query.AsQueryString()}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
        
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var deserialized = JsonSerializer.Deserialize<ZohoQualifiedLeadResponse>(json, options);
        
            if (deserialized?.Data != null)
                leads.AddRange(deserialized.Data);
            
            moreRecords = deserialized?.Info.MoreRecords ?? false;
            
        } while (moreRecords && ++query.Page > 0); 
        
        return leads;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var url = "https://accounts.zoho.com/oauth/v2/token";
        var formData = new[]
        {
            new KeyValuePair<string, string>("client_id", offlineConversionsJobSettings.CurrentValue.ZohoClientId),
            new KeyValuePair<string, string>("client_secret", offlineConversionsJobSettings.CurrentValue.ZohoClientSecret),
            new KeyValuePair<string, string>("refresh_token", offlineConversionsJobSettings.CurrentValue.ZohoRefreshToken),
            new KeyValuePair<string, string>("grant_type", "refresh_token")
        };

        var content = new FormUrlEncodedContent(formData);
        var response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<ZohoTokenResponse>(json);

        return tokenResponse?.AccessToken;
    }
    
    public async Task<int> UpdateZohoQualifiedLeadsAsync(string accessToken, List<UpdateZohoQualifiedLead> leads)
    {
        var url = "https://www.zohoapis.com/crm/v8/Leads";
        
        var requestBody = new
        {
            data = leads
        };
        
        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody));
        jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);
        request.Content = jsonContent;
        
        var response = await httpClient.SendAsync(request);
        try
        {
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var deserialized = JsonSerializer.Deserialize<UpdateZohoQualifiedLeadResponse>(json, options);

            if (deserialized.Data.All(d => d.Code == "SUCCESS"))
            {
                logger.LogInformation("Successfully updated {LeadCount} leads in Zoho.", leads.Count);
                return deserialized.Data.Count;
            }

            if (deserialized.Data.All(d => d.Code != "SUCCESS"))
            {
                logger.LogError("Failed to update all leads in Zoho.");
                return 0;
            }

            foreach (var result in deserialized.Data.Where(d => d.Code != "SUCCESS"))
            {
                logger.LogError("Failed to update lead with Id {LeadId}. Message: {Message}", result.Details.Id,
                    result.Message);
            }

            return deserialized.Data.Count(d => d.Code == "SUCCESS");
        }
        catch (Exception ex)
        {
            logger.LogError("Skipping chunk. Exception occurred while updating leads in Zoho: {ExceptionMessage}", ex.Message);
            return 0;
        }
    }
}