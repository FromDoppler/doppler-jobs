using System.Collections.Generic;
using System.Threading.Tasks;
using OfflineConversionsJob.Entities;

namespace OfflineConversionsJob.ApiClient;

public interface IZohoApiClient
{
    Task<List<ZohoQualifiedLead>> GetZohoQualifiedLeadsAsync(string accessToken);
    Task<string> GetAccessTokenAsync();
    Task<int> UpdateZohoQualifiedLeadsAsync(string accessToken, List<UpdateZohoQualifiedLead> leadsToUpdate);
}