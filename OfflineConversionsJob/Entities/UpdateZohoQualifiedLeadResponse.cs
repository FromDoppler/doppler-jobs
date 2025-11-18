using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OfflineConversionsJob.Entities;

public class UpdateZohoQualifiedLeadResponse
{
    public List<UpdateZohoQualifiedLeadDataResponse> Data { get; set; }
}

public class UpdateZohoQualifiedLeadDataResponse
{
    public string Code { get; set; }
    public UpdateZohoQualifiedLeadDetailsResponse Details { get; set; }
    public string Message { get; set; }
    public string Status { get; set; }
}

public class UpdateZohoQualifiedLeadDetailsResponse
{
    [JsonPropertyName("Modified_Time")]
    public string ModifiedTime { get; set; }
    
    [JsonPropertyName("Modified_By")]
    public object ModifiedBy { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("Created_Time")]
    public string CreatedTime { get; set; }
    
    [JsonPropertyName("Created_By")]
    public object CreatedBy { get; set; }
}